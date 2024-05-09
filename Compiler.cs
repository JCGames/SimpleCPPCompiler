using System.Drawing;

internal class Compiler(FileTable fileTable)
{
    private readonly Dictionary<string, FilePointer> _dependencies = [];

    public void Compile(string rootFileName)
    {
        // find the main file
        foreach (var file in fileTable)
        {
            if (rootFileName == file.Name)
            {
                var filesToCompile = GetFilesForCompilation(file);

                // run the compilation
                var process = new System.Diagnostics.Process();
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                process.StartInfo.FileName = "cmd.exe";

                var executableFileCommand = $"/C cd {file.Directory} && g++ " + file.Name + " ";

                foreach (var fileToCompile in filesToCompile)
                {
                    var objectFileName = fileToCompile.Name[0..(fileToCompile.Name.Length - Path.GetExtension(fileToCompile.Name).Length)] + ".o";

                    // continue building executable command
                    executableFileCommand += (file.Directory != fileToCompile.Directory ? Path.GetRelativePath(file.Directory, fileToCompile.Directory) : "") + objectFileName + " ";

                    if (!fileToCompile.ShouldBeCompiled) continue;

                    // compile object file
                    var objectFileCommand = $"/C cd {fileToCompile.Directory} && g++ -c {fileToCompile.Name}";
                    Console.WriteLine(objectFileCommand);

                    process.StartInfo.Arguments = objectFileCommand;
                    process.Start();
                    process.WaitForExit();
                }

                // run exectuable command
                executableFileCommand += "-o " + file.Name[0..(file.Name.Length - Path.GetExtension(file.Name).Length)];
                Console.WriteLine(executableFileCommand);
                
                process.StartInfo.Arguments = executableFileCommand;
                process.Start();
                process.WaitForExit();
                break;
            }
        }
    }

    public void CleanFiles()
    {
        foreach (var file in fileTable)
        {
            if (Path.GetExtension(file.Name) != ".o" && Path.GetExtension(file.Name) != ".exe")
                continue;

            var process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.StartInfo.FileName = "cmd.exe";

            var command = $"/C del \"" + file.Directory + file.Name + "\"";
            Console.WriteLine(command);

            process.StartInfo.Arguments = command;
            process.Start();
            process.WaitForExit();
        }
    }

    public List<FilePointer> GetFilesForCompilation(FilePointer rootFile)
    {
        List<FilePointer> modifiedFilesForCompilation = [];
        
        SetDependencies(rootFile);

        const string OBJECT_FILE_EXT = ".o";
        const string SOURCE_FILE_EXT = ".cpp";

        foreach (var dependency in _dependencies.Values)
        {
            var ext = Path.GetExtension(dependency.Name);
            var nameNoExt = dependency.Name[0..(dependency.Name.Length - ext.Length)];

            // if the header file has an .o file and .cpp file
            if (fileTable.Contains(dependency.Directory + nameNoExt + OBJECT_FILE_EXT) &&
                fileTable.Contains(dependency.Directory + nameNoExt + SOURCE_FILE_EXT))
            {
                var oFile = fileTable[dependency.Directory + nameNoExt + OBJECT_FILE_EXT];
                var cppFile = fileTable[dependency.Directory + nameNoExt + SOURCE_FILE_EXT];

                // if the source file or header file have been modified then recompile the object file
                if (oFile.ModifiedDate < dependency.ModifiedDate || oFile.ModifiedDate < cppFile.ModifiedDate)
                    cppFile.ShouldBeCompiled = true;

                modifiedFilesForCompilation.Add(cppFile);
            }
            // if the header file only has a .cpp file
            else if (fileTable.Contains(dependency.Directory + nameNoExt + SOURCE_FILE_EXT))
            {
                var cppFile = fileTable[dependency.Directory + nameNoExt + SOURCE_FILE_EXT];

                cppFile.ShouldBeCompiled = true;
                modifiedFilesForCompilation.Add(cppFile);
            }
            else
            {
                FilePointer? oFile = null;
                FilePointer? cppFile = null;

                foreach (var file in fileTable)
                {
                    if (file.Name == nameNoExt + OBJECT_FILE_EXT)
                        oFile = file;
                    else if (file.Name == nameNoExt + SOURCE_FILE_EXT)
                        cppFile = file;
                }

                // if the header file has an .o file and .cpp file
                if (cppFile != null && oFile != null)
                {   
                    // if the source file or header file have been modified then recompile the object file
                    if (oFile.ModifiedDate < dependency.ModifiedDate || oFile.ModifiedDate < cppFile.ModifiedDate)
                        cppFile.ShouldBeCompiled = true;

                    modifiedFilesForCompilation.Add(cppFile);
                }
                // if the header file only has a .cpp file
                else if (cppFile != null)
                {
                    cppFile.ShouldBeCompiled = true;
                    modifiedFilesForCompilation.Add(cppFile);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[WARNING] " + dependency.Name + " is a single header file.");
                    Console.ResetColor();
                }
            }
        }

        return modifiedFilesForCompilation;
    }

    public void SetDependencies(FilePointer rootFile)
    {
        foreach (var filePath in rootFile.Dependencies)
        {
            if (fileTable.Contains(filePath))
            {
                _dependencies.TryAdd(filePath, fileTable[filePath]);
                SetDependencies(fileTable[filePath]);
            }
        }
    }
}