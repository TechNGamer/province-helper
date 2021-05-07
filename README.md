# Province Helper
Province Helper is a program that will copy province files from the auto-detected Europa Universalis IV location.
This program utilizes .NET 5 and Windows Presentation Foundation.
It also has a basic line-by-line compare and a view for helping compare 2 files.

## Auto detection of the game
This program looks into the Windows registry to locate where the Steam installation location is.
It will then open up the `libraryfolders.vdf` file and look for the index numbers that represent directories.
It will then iterate over those indexes looking for a folder called `Europa Universalis IV` by appending `steamapps/common` to the path.

## The Save Space format
Save spaces are a crucial part of the program. These files let you save the destination location (which is optional) and the provinces that you wish to copy.
There are 2 choices of save spaces, one being a text file while the other is a binary file.
The binary option is recommended as it is able to quickly parse the data, while the text version has to do some string manipulation.

## Logging
The program logs things Asynchronously. All log messages are put into a queue, while another task is emptying that queue out to a log file.
The program also makes sures that all logs are written out before the program closes.
