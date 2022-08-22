using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MsDelta;
using System.Net.Http;
using System.Collections.Concurrent;

namespace DeltaDownloader
{
    internal class Program
    {
        private const uint PAGE_SIZE = 0x1000;
        private static readonly HttpClientHandler s_clientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
        private static readonly HttpClient s_client = new HttpClient(s_clientHandler);
        private static readonly ConcurrentBag<string> s_urls = new ConcurrentBag<string>();
        private static readonly ConcurrentBag<(string, string, DeltaFile)> s_files = new ConcurrentBag<(string, string, DeltaFile)>();
        private static async Task<bool> SymbolUrlValid(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            while (true)
            {
                var response = await s_client.SendAsync(request);
                var intStatus = (int)response.StatusCode;
                if (intStatus >= 500 && intStatus < 600)
                {
                    await Task.Delay(100);
                    continue;
                }
                return response.StatusCode == System.Net.HttpStatusCode.Found;
            }
        }

        private static ulong GetMappedSize(ulong size)
        {
            const ulong PAGE_MASK = (PAGE_SIZE - 1);
            var page = size & ~PAGE_MASK;
            if (page == size) return page;
            return page + PAGE_SIZE;
        }

        private static string CreateUrl(string filename, uint TimeDateStamp, uint SizeOfImage)
        {
            return string.Format("https://msdl.microsoft.com/download/symbols/{0}/{1:x8}{2:x}/{0}", filename, TimeDateStamp, SizeOfImage);
        }

        private static void ParseDelta(string path, string dir)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                var delta = new DeltaFile(bytes);
                var filename = Path.GetFileName(path);
                // https://msdl.microsoft.com/download/symbols/cdboot_noprompt.efi/{0:X8}{1:X}/cdboot_noprompt.efi, TimeDateStamp, SizeOfImage
                if ((delta.Code & FileTypeCode.Raw) != 0) return;
                if (delta.FileTypeHeader?.RiftTable == null) return;
                s_files.Add((dir, filename, delta));
            } catch { }
        }

        private static async Task<string> GetUrlFromDelta((string, string, DeltaFile) deltaPair)
        {
            var filename = deltaPair.Item2;
            var delta = deltaPair.Item3;

            var timeDateStamp = delta.FileTypeHeader.TimeStamp;
            // We use the rift table (VirtualAddress,PointerToRawData pairs for each section) and the target file size to calculate the SizeOfImage.
            var lastSection = delta.FileTypeHeader.RiftTable.Last();
            var lastSectionAndSignatureSize = delta.TargetSize - lastSection.Value;
            var lastSectionMapped = lastSection.Key;
            var lastSectionAndSignatureMappedSize = GetMappedSize(lastSectionMapped + lastSectionAndSignatureSize);

            uint sizeOfImage = (uint)lastSectionAndSignatureMappedSize;
            uint lowestSizeOfImage = (uint)lastSectionMapped + PAGE_SIZE;

            var urls = new List<string>();
            var tasks = new List<Task<bool>>();
            for (uint size = sizeOfImage; size >= lowestSizeOfImage; size -= PAGE_SIZE)
            {
                string url = CreateUrl(filename, timeDateStamp, size);
                urls.Add(url);
                tasks.Add(Task.Run(() => SymbolUrlValid(url)));
            }
            await Task.WhenAll(tasks);
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].Result) return urls[i];
            }
            throw new InvalidDataException();
        }

        static async Task ProcessDelta((string, string, DeltaFile) delta)
        {
            try
            {
                var url = await GetUrlFromDelta(delta);
                var sb = new StringBuilder(url);
                sb.AppendLine();
                sb.AppendFormat(" out={0}", delta.Item2);
                s_urls.Add(sb.ToString());
            }
            catch { }
        }

        static void Process(string directory, string parent, bool inDir = false)
        {
            try
            {
                foreach (var folder in Directory.EnumerateDirectories(directory))
                {
                    Process(folder, directory, inDir || folder.Substring(directory.Length + 1) == "f");
                }
                if (!inDir) return;
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    if (Path.GetExtension(file) == "mui") continue;
                    ParseDelta(file, parent);
                }
                Console.Write(".");
            } catch { }
        }

        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: {0} <dir>", Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));
                Console.WriteLine();
                Console.WriteLine("Given a folder containing delta compressed PE files from a Windows update package,");
                Console.WriteLine("uses the data in the delta compression header to search for the PE files on the MS symbol server.");
                Console.WriteLine();
                return;
            }
            var tasks = new List<Task>();
            Console.Write("Searching for delta files");
            Process(args[0], args[0]);
            Console.WriteLine();
            Console.WriteLine("Attempting to find {0} files on the Microsoft Symbol Server...", s_files.Count);
            foreach (var file in s_files)
            {
                tasks.Add(Task.Run(() => ProcessDelta(file)));
            }
            var whenAll = Task.WhenAll(tasks);
            while (true)
            {
                await Task.WhenAny(whenAll, Task.Delay(1000));
                if (whenAll.IsCompleted) break;
                var tasksCompletedCount = tasks.Where((t) => t.IsCompleted).Count();
                double percent = 100.0 * tasksCompletedCount / tasks.Count;
                Console.Write("{0}% complete ({1}/{2})\r", Math.Round(percent, 2), tasksCompletedCount, tasks.Count);
            }
            Console.WriteLine();

            var sb = new StringBuilder();
            foreach (var aria in s_urls)
            {
                sb.AppendLine(aria);
            }
            var ariaPath = Path.Combine(args[0], "aria2.txt");
            File.WriteAllText(ariaPath, sb.ToString());
            Console.WriteLine("Written aria2 input file to {0}", ariaPath);
        }
    }
}
