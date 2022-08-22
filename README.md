# DeltaDownloader

Given a directory containing delta compressed PE files from a Windows update package, uses the data in the delta compression header to search for the PE files on the Microsoft symbol server. All found URLs will be written to an aria2 input text file.

## Theory of operation

The delta compression header for a PE file contains the following information:
- Size of output file
- `TimeDateStamp` of output file
- `(VirtualAddress, PointerToRawData)` for each section of output file

PE files on the Microsoft symbols server are stored with keys of `(OriginalFileName, TimeDateStamp, SizeOfImage)`.

Two of those are known (the original file name being the filename of the delta compressed file, the TimeDateStamp in the delta compression header).

The other can be determined from the size of the output file and the VirtualAddress/PointerToRawData of the last section:
- `OutputSize - Sections.Last().PointerToRawData` is the size of the last section plus PE signatures.
- This value can be rounded up to a page size, leading to a low number of potential `SizeOfImage` values to check.
- These can be checked by HEAD requests to the Microsoft symbols server: `302` means the correct value was discovered.

Enough of the functionality of `msdelta.dll` has been reimplemented in C# to allow for obtaining the required values from the delta compression header.

## Practical details

Running this on the extracted update package is possible, but unwise (several thousand binaries would take some time to check, `HttpClient` is not DirBuster).

Running this on the specific directories of the update package containing components you want to bindiff is the better option.

