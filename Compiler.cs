internal class Compiler(FileTable fileTable)
{
    private const string OBJECT_FILE_EXT = ".o";
    private const string SOURCE_FILE_EXT = ".cpp";
    private const string HEADER_FILE_EXT = ".hpp";

    private readonly Dictionary<string, FilePointer> _dependencies = [];
    private readonly Dictionary<string, FilePointer> _sourceFileIndex = [];

    /// <summary>
    /// Generates the necessary commands to compile a c++ project.
    /// </summary>
    /// <param name="rootFileName">The root c++ file. Usually called "main.cpp".</param>
    public void Compile(string rootFileName)
    {
        // find the main file
        foreach (var mainSourceFile in fileTable)
        {
            if (rootFileName == mainSourceFile.Name)
            {
                var sourceFilesToCompile = GetSourceFilesForCompilation(mainSourceFile);

                // run the compilation
                var process = GetCmdProcess();

                foreach (var sourceFile in sourceFilesToCompile)
                {                    
                    if (!sourceFile.ShouldBeCompiled) continue;

                    var objectFileCommand = GenerateObjectFileCommand(sourceFile);
                    Console.WriteLine(objectFileCommand);

                    RunCmdCommand(process, objectFileCommand);
                }

                var executableFileCommand = GenerateExecutableFileCommand(mainSourceFile, sourceFilesToCompile);
                Console.WriteLine(executableFileCommand);
                
                RunCmdCommand(process, executableFileCommand);
                break;
            }
        }
    }

    /// <summary>
    /// Windows only.
    /// </summary>
    public void CleanFiles()
    {
        foreach (var file in fileTable)
        {
            if (Path.GetExtension(file.Name) != ".o" && Path.GetExtension(file.Name) != ".exe")
                continue;

            var process = GetCmdProcess();

            var command = $"/C del \"" + file.Directory + file.Name + "\"";
            Console.WriteLine(command);

            RunCmdCommand(process, command);
        }
    }

    private static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[WARNING] " + message);
        Console.ResetColor();
    }

    private static void PrintError(string message, int errorExitCode)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[ERROR] " + message);
        Console.ResetColor();
        Environment.Exit(errorExitCode);
    }

    private static System.Diagnostics.Process GetCmdProcess() => new()
    {
        StartInfo = new()
        {
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            FileName = "cmd.exe"
        }
    };

    private static void RunCmdCommand(System.Diagnostics.Process process, string command)
    {
        process.StartInfo.Arguments = command;
        process.Start();
        process.WaitForExit();
    }

    private static string RemoveExtension(string fileName) =>
        fileName[0..(fileName.Length - Path.GetExtension(fileName).Length)];

    private static string GetSourceFileName(FilePointer filePointer) => 
        RemoveExtension(filePointer.Name) + SOURCE_FILE_EXT;

    private static string GetObjectFileName(FilePointer filePointer) => 
        RemoveExtension(filePointer.Name) + OBJECT_FILE_EXT;

    private static string GetHeaderFileName(FilePointer filePointer) =>
        RemoveExtension(filePointer.Name) + HEADER_FILE_EXT;

    private static string GenerateObjectFileCommand(FilePointer sourceFilePointer) =>
        $"/C cd {sourceFilePointer.Directory} && g++ -c {sourceFilePointer.Name}";

    private static string HeaderFilePathToSourceFilePath(string headerFilePath)
    {
        string fileName = Path.GetFileName(headerFilePath);

        var sourceFilePath = headerFilePath[0..(headerFilePath.Length - fileName.Length)];

        var fileNameNoExt = RemoveExtension(fileName);

        sourceFilePath += fileNameNoExt + SOURCE_FILE_EXT;

        return sourceFilePath;
    }

    private static string GenerateExecutableFileCommand(FilePointer mainSourceFile, IEnumerable<FilePointer> sourceFiles)
    {
        var executableFileCommand = $"/C cd {mainSourceFile.Directory} && g++ " + mainSourceFile.Name + " ";

        foreach (var sourceFile in sourceFiles)
        {
            var objectFileDirectory = mainSourceFile.Directory != sourceFile.Directory ? Path.GetRelativePath(mainSourceFile.Directory, sourceFile.Directory) : "";
            executableFileCommand += objectFileDirectory + GetObjectFileName(sourceFile) + " ";
        }
        
        executableFileCommand += "-o " + RemoveExtension(mainSourceFile.Name);

        return executableFileCommand;
    }

    private List<FilePointer> GetSourceFilesForCompilation(FilePointer rootFile)
    {
        List<FilePointer> sourceFilesForCompilation = [];
        
        SetDependencies(rootFile);

        foreach (var dependency in _dependencies.Values)
        {
            var nameNoExt = RemoveExtension(dependency.Name);

            // if the header file has an .o file and .cpp file
            if (fileTable.Contains(dependency.Directory + nameNoExt + OBJECT_FILE_EXT) &&
                fileTable.Contains(dependency.Directory + nameNoExt + SOURCE_FILE_EXT))
            {
                var oFile = fileTable[dependency.Directory + nameNoExt + OBJECT_FILE_EXT];
                var sourceFile = fileTable[dependency.Directory + nameNoExt + SOURCE_FILE_EXT];

                // if the source file or header file have been modified then recompile the object file
                if (oFile.ModifiedDate < dependency.ModifiedDate || oFile.ModifiedDate < sourceFile.ModifiedDate)
                    sourceFile.ShouldBeCompiled = true;

                sourceFilesForCompilation.Add(sourceFile);
            }
            // if the header file only has a .cpp file
            else if (fileTable.Contains(dependency.Directory + nameNoExt + SOURCE_FILE_EXT))
            {
                var sourceFile = fileTable[dependency.Directory + nameNoExt + SOURCE_FILE_EXT];

                sourceFile.ShouldBeCompiled = true;
                sourceFilesForCompilation.Add(sourceFile);
            }
            // The header and source files were not in the same folder so we have to resort
            // to a cruder look up.
            else
            {
                fileTable.TryFindFileByNameSlow(nameNoExt + OBJECT_FILE_EXT, out var oFile);
                fileTable.TryFindFileByNameSlow(nameNoExt + SOURCE_FILE_EXT, out var sourceFile);

                // if the header file has an .o file and .cpp file
                if (sourceFile != null && oFile != null)
                {   
                    // if the source file or header file have been modified then recompile the object file
                    if (oFile.ModifiedDate < dependency.ModifiedDate || oFile.ModifiedDate < sourceFile.ModifiedDate)
                        sourceFile.ShouldBeCompiled = true;

                    sourceFilesForCompilation.Add(sourceFile);
                }
                // if the header file only has a .cpp file
                else if (sourceFile != null)
                {
                    sourceFile.ShouldBeCompiled = true;
                    sourceFilesForCompilation.Add(sourceFile);
                }
                else
                {
                    if (!rootFile.Options.NoWarn)
                        PrintWarning(dependency.Name + " is a single header file.");
                }
            }
        }

        return sourceFilesForCompilation;
    }

    private void SetDependencies(FilePointer sourceFile)
    {
        sourceFile.HaveDependenciesBeenTouched = true;

        // just allows for faster source file look up
        IndexSourceFilesByName();

        foreach (var headerFilePath in sourceFile.Dependencies)
        {
            if (Path.GetExtension(headerFilePath) != HEADER_FILE_EXT)
            {
                PrintError($"Dependency \"{headerFilePath}\" must be a header file path in \"{sourceFile.Name}\".", 1);
            }

            if (fileTable.Contains(headerFilePath))
            {
                var headerFile = fileTable[headerFilePath];
                _dependencies.TryAdd(headerFilePath, headerFile);

                var sourceFilePath = HeaderFilePathToSourceFilePath(headerFilePath);
                var sourceFileName = Path.GetFileNameWithoutExtension(headerFilePath) + SOURCE_FILE_EXT;

                // If the header file has a source file related to it,
                // then dependencies from that source file should have
                // it's dependencies included as well.
                if (fileTable.Contains(sourceFilePath))
                {
                    if (!fileTable[sourceFilePath].HaveDependenciesBeenTouched)
                        SetDependencies(fileTable[sourceFilePath]);
                }
                else if (_sourceFileIndex.ContainsKey(sourceFileName))
                {
                    if (!_sourceFileIndex[sourceFileName].HaveDependenciesBeenTouched)
                        SetDependencies(_sourceFileIndex[sourceFileName]);
                }

                // If this files dependencies have already been added,
                // then just skip this file.
                if (!headerFile.HaveDependenciesBeenTouched) 
                {
                    SetDependencies(headerFile);
                }
            }
        }
    }

    private void IndexSourceFilesByName()
    {
        foreach (var file in fileTable)
        {
            if (Path.GetExtension(file.Name) == SOURCE_FILE_EXT)
                _sourceFileIndex.TryAdd(file.Name, file);
        }
    }
}