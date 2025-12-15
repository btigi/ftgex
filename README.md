# FtgEx

A command-line app to manipulate FTG files from the 1997 RTS game Dark Reign by Auren.

## Usage

FtgEx is a command line application and should be run from a terminal session. Application usage is

```
Usage: 
  ftgex.exe --extract, -e <archive.ftg>
  ftgex.exe --create, -c <folder1> [folder2] ...
  ftgex.exe --help, -h
```


Usage examples:

 ```ftgex.exe --extract data.ftg```

 ```ftgex.exe -e ships.ftg```

 ```ftgex.exe --create mymod```

 ```ftgex.exe -c folder1 folder2 folder3```
 
## Requirements

- .NET 10.0
- Windows OS

## Compiling

To clone and run this application, you'll need [Git](https://git-scm.com) and [.NET](https://dotnet.microsoft.com/) installed on your computer. From your command line:

```
# Clone this repository
$ git clone https://github.com/btigi/ftgex

# Go into the repository
$ cd src

# Build  the app
$ dotnet build
```

## Licencing

FtgEx is licensed under the MIT license. Full licence details are available in license.md
