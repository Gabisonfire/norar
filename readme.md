## norar

norar is a small utility built on 7zip that will decompress archives in the specified directory silently.



Compatible formats are: 7z, XZ, BZIP2, GZIP, TAR, ZIP, WIM, AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.


**Basic Usage:**

    norar "top_dir" "dest_dir" "rar,zip,7z" [switches]

or

    norar usecfg "C:\Path\to\config"


**Switches:**

| Switch   |  Descritption  | Example |
| :------- | :------------- | :------ |
| /buildhashes | Builds the hashes file without decompressing anything. Useful on first run if you don't need the existing files decompressed. |
| /dir | Will create a different directory for each extract using the archive name. |
| /fullpath | Shows full archive paths in console when extracting. |
| /recursive | Will search recursively from top_dir |
| /overwrite | Will overwrite existing files |
| /matchfile | Will ommit the files contained in .hashes by their file names only. Faster but more error prone. (Matches hashes only by default) |
| /force  | Will force the program to reprocess the files even if they match the hashes file. |
| /halt | Will halt the program before exiting. |
| /regexclude:regex_pattern | Will exclude any files matching that pattern. | /regexclude:a.+?d |
| /dryrun | Will run the application without decompressing anything or storing hashes. |
| /purgesize:size | Sets the max size in MB of the hashes files before it gets purged. Default: 10 | /purgesize:5 |
| /purgebacklog:lines | Sets the number of lines to keep when purging hashes. Default: 200 | /purgebacklog:500 |
| /logpath:path | Path to save log file. Default is executable directory | /logpath:C:\mylogs\ |
| /logsize:size | Max size of log file in MB. Default: 10 | /logsize:5 |
| /movelog | When purging will move the log files instead of deleting them. |
| /movehashes | When purging will move the hashes files instead of deleting them. (Still applying /purgebacklog before deletion) |

**Config file** structure is the same as the command line version, following the same order. One on each line. Example:

    C:\archives
    C:\decompressed
    rar,zip,7z
    /dir
    /fullpath
    /recursive
    /regexclude:a.+?d


**Exit codes:**
- 0: Everything went fine
- 1: Directory does not exist
- 2: Invalid arguments
- 3: Unable to read config file
- 4: Invalid config file syntax