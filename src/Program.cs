using ii.BrightRespite;

namespace ftgex;

class Program
{
    private static readonly string[] SupportedExtensions = [".ftg"];

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var firstArg = args[0].ToLowerInvariant();

        if (firstArg == "--help" || firstArg == "-h")
        {
            ShowHelp();
            return 0;
        }

        if (firstArg == "--extract" || firstArg == "-e")
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Error: No archive file specified for extraction.");
                Console.Error.WriteLine("Usage: ftgex.exe --extract <archive.ftg>");
                return 1;
            }
            return HandleExtract(args.Skip(1).ToArray());
        }

        if (firstArg == "--create" || firstArg == "-c")
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Error: No folder specified for FTG creation.");
                Console.Error.WriteLine("Usage: ftgex.exe --create <folder1> [folder2] ...");
                return 1;
            }
            return HandleCreate(args.Skip(1).ToArray());
        }

        ShowHelp();
        return 0;
    }

    static void ShowHelp()
    {
        var helpText = @"FtgEx

Usage:
  ftgex.exe --extract, -e <archive.ftg>
  ftgex.exe --create, -c <folder1> [folder2] ...
  ftgex.exe --help, -h

Examples:
  ftgex.exe --extract data.ftg
  ftgex.exe -e ships.ftg
  ftgex.exe --create mymod
  ftgex.exe -c folder1 folder2 folder3

Extract: Files are extracted to a folder named after the archive.
Create:  Single folder - contents added without folder name prefix.
         Multiple folders - combined into one FTG with relative paths.";

        Console.WriteLine(helpText);
    }

    static int HandleExtract(string[] args)
    {
        var hasErrors = false;

        foreach (var arg in args)
        {
            try
            {
                var filePath = arg.Trim('"');
                
                if (!File.Exists(filePath))
                {
                    Console.Error.WriteLine($"Error: File not found: {filePath}");
                    hasErrors = true;
                    continue;
                }

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (!SupportedExtensions.Contains(extension))
                {
                    Console.Error.WriteLine($"Error: Unsupported file type: {extension}");
                    Console.Error.WriteLine($"Supported types: {string.Join(", ", SupportedExtensions)}");
                    hasErrors = true;
                    continue;
                }

                var (success, message) = ExtractArchive(filePath);
                Console.WriteLine(message);
                if (!success) hasErrors = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing {arg}: {ex.Message}");
                hasErrors = true;
            }
        }

        return hasErrors ? 1 : 0;
    }

    static int HandleCreate(string[] args)
    {
        var paths = args.Select(a => a.Trim('"').TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).ToArray();

        foreach (var path in paths)
        {
            if (!Directory.Exists(path))
            {
                Console.Error.WriteLine($"Error: Folder not found: {path}");
                return 1;
            }
        }

        try
        {
            var (success, message) = CreateFtgArchive(paths);
            Console.WriteLine(message);
            return success ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating FTG: {ex.Message}");
            return 1;
        }
    }

    static (bool success, string message) ExtractArchive(string archivePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(archivePath);
        var directory = Path.GetDirectoryName(archivePath) ?? Environment.CurrentDirectory;
        var outputFolder = Path.Combine(directory, fileName);

        Console.WriteLine($"Extracting: {archivePath}");
        Console.WriteLine($"Output folder: {outputFolder}");

        var ftgProcessor = new FtgProcessor();
        var files = ftgProcessor.Read(archivePath);

        if (files == null || files.Count == 0)
        {
            return (false, $"{fileName}: No files found in archive.");
        }

        Console.WriteLine($"Found {files.Count} file(s) in archive.");

        Directory.CreateDirectory(outputFolder);

        var extractedCount = 0;
        var errors = new List<string>();

        foreach (var (relativePath, fileData) in files)
        {
            try
            {
                // Normalize path separators and remove leading backslash if present
                var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                normalizedPath = normalizedPath.TrimStart(Path.DirectorySeparatorChar);

                var outputPath = Path.Combine(outputFolder, normalizedPath);
                var outputDir = Path.GetDirectoryName(outputPath);

                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                File.WriteAllBytes(outputPath, fileData);
                extractedCount++;
                Console.WriteLine($"  Extracted: {normalizedPath}");
            }
            catch (Exception ex)
            {
                errors.Add($"{relativePath}: {ex.Message}");
            }
        }

        var result = $"{fileName}: Extracted {extractedCount}/{files.Count} files to:\n{outputFolder}";
        
        if (errors.Count > 0)
        {
            result += $"\n\nErrors ({errors.Count}):\n" + string.Join("\n", errors.Take(5));
            if (errors.Count > 5)
                result += $"\n... and {errors.Count - 5} more errors";
        }

        Console.WriteLine($"Extraction complete. {extractedCount}/{files.Count} files extracted to: {outputFolder}");

        return (errors.Count == 0, result);
    }

    static (bool success, string message) CreateFtgArchive(string[] folderPaths)
    {
        var entries = new List<(string filename, byte[] bytes)>();
        var errors = new List<string>();

        string outputPath;
        string baseDirectory;

        if (folderPaths.Length == 1)
        {
            var folderPath = folderPaths[0];
            var folderName = Path.GetFileName(folderPath);
            var parentDir = Path.GetDirectoryName(folderPath) ?? Environment.CurrentDirectory;
            
            outputPath = Path.Combine(parentDir, folderName + ".ftg");
            baseDirectory = folderPath;

            Console.WriteLine($"Creating FTG from folder: {folderPath}");
            Console.WriteLine($"Output: {outputPath}");

            foreach (var filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var relativePath = Path.GetRelativePath(baseDirectory, filePath);
                    // FTG files use backslash paths with leading backslash
                    relativePath = "\\" + relativePath.Replace(Path.DirectorySeparatorChar, '\\');
                    var data = File.ReadAllBytes(filePath);
                    
                    entries.Add((relativePath, data));

                    Console.WriteLine($"  Adding: {relativePath}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{filePath}: {ex.Message}");
                }
            }
        }
        else
        {
            var commonParent = GetCommonParentDirectory(folderPaths);
            var parentName = Path.GetFileName(commonParent);
            
            if (string.IsNullOrEmpty(parentName))
            {
                parentName = "archive";
            }

            outputPath = Path.Combine(commonParent, parentName + ".ftg");
            baseDirectory = commonParent;

            Console.WriteLine($"Creating FTG from {folderPaths.Length} folder(s)");
            Console.WriteLine($"Common parent: {commonParent}");
            Console.WriteLine($"Output: {outputPath}");

            foreach (var folderPath in folderPaths)
            {
                Console.WriteLine($"  Processing folder: {folderPath}");

                foreach (var filePath in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(baseDirectory, filePath);
                        // FTG files use backslash paths with leading backslash
                        relativePath = "\\" + relativePath.Replace(Path.DirectorySeparatorChar, '\\');
                        var data = File.ReadAllBytes(filePath);
                        
                        entries.Add((relativePath, data));

                        Console.WriteLine($"    Adding: {relativePath}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{filePath}: {ex.Message}");
                    }
                }
            }
        }

        if (entries.Count == 0)
        {
            return (false, "No files found in the selected folder(s).");
        }

        if (File.Exists(outputPath))
        {
            var dir = Path.GetDirectoryName(outputPath) ?? ".";
            var name = Path.GetFileNameWithoutExtension(outputPath);
            var ext = Path.GetExtension(outputPath);
            var counter = 1;
            
            while (File.Exists(outputPath))
            {
                outputPath = Path.Combine(dir, $"{name}_{counter}{ext}");
                counter++;
            }

            Console.WriteLine($"Output file exists, using: {Path.GetFileName(outputPath)}");
        }

        Console.WriteLine($"Writing FTG archive with {entries.Count} file(s)...");

        var ftgProcessor = new FtgProcessor();
        ftgProcessor.Write(outputPath, entries);

        var result = $"Created: {Path.GetFileName(outputPath)}\n" +
                     $"Location: {Path.GetDirectoryName(outputPath)}\n" +
                     $"Files: {entries.Count}";

        if (errors.Count > 0)
        {
            result += $"\n\nErrors ({errors.Count}):\n" + string.Join("\n", errors.Take(5));
            if (errors.Count > 5)
                result += $"\n... and {errors.Count - 5} more errors";
        }

        Console.WriteLine($"FTG creation complete: {Path.GetFileName(outputPath)}");

        return (errors.Count == 0, result);
    }

    static string GetCommonParentDirectory(string[] paths)
    {
        if (paths.Length == 0)
            return string.Empty;

        if (paths.Length == 1)
            return Path.GetDirectoryName(paths[0]) ?? paths[0];

        var pathParts = paths
            .Select(p => Path.GetFullPath(p).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .ToList();

        var commonParts = new List<string>();
        var minLength = pathParts.Min(p => p.Length);

        for (int i = 0; i < minLength; i++)
        {
            var part = pathParts[0][i];
            if (pathParts.All(p => p[i].Equals(part, StringComparison.OrdinalIgnoreCase)))
            {
                commonParts.Add(part);
            }
            else
            {
                break;
            }
        }

        if (commonParts.Count == 0)
            return string.Empty;

        var result = string.Join(Path.DirectorySeparatorChar.ToString(), commonParts);
        
        if (result.Length == 2 && result[1] == ':')
            result += Path.DirectorySeparatorChar;

        return result;
    }
}
