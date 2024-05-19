using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

internal partial class CommandsBuilder(FileIndexTable fileIndexTable)
{
    private readonly FileIndexTable indexTable = fileIndexTable;
    private readonly HashSet<FileIndex> _dependencies = [];

    [GeneratedRegex("#include.*\"(?<path>.*)\"")]
    private static partial Regex IncludeRegex();

    public void GenerateMakeFile(string rootSourceFileName)
    {
        var rootSourceFileExtension = Path.GetExtension(rootSourceFileName);

        if (rootSourceFileExtension != ".cpp" && rootSourceFileExtension != ".c")
        {
            Console.WriteLine("The root file to compile from should be a source file: " + rootSourceFileName);
            Environment.Exit(0);
        }

        if (indexTable.TryGet(rootSourceFileName, out var fileIndex))
        {
            var path = Path.GetDirectoryName(fileIndex!.Path);

            if (path == null)
                return;
            
            GetAllDependencies(fileIndex!);
            List<FileIndex> dependenciesWithSourceFile = []; // we only want to compile source files into object files

            foreach (var dependency in _dependencies)
            {
                if (indexTable.ContainsKey(Path.ChangeExtension(dependency.Name, ".cpp")))
                    dependenciesWithSourceFile.Add(dependency);
            }

            // open file
            FileStream? fileStream;

            if (!File.Exists(path + "\\Makefile"))
            {
                fileStream = File.Create(path + "\\Makefile");
            }
            else 
            {
                fileStream = File.Open(path + "\\Makefile", FileMode.Truncate);
            }

            // write to file
            using var writer = new StreamWriter(fileStream);

            // write the executable compilation part
            writer.Write(Path.GetFileNameWithoutExtension(fileIndex.Name) + ": " + fileIndex.Name + " ");

            foreach (var dependency in dependenciesWithSourceFile)
            {
                string relativePath = Path.GetRelativePath(path, dependency.Path);
                writer.Write(Path.ChangeExtension(relativePath, ".o") + " ");
            }

            // write the dependencies object file compilation part
            writer.Write("\n\tg++ $^ -o " + Path.GetFileNameWithoutExtension(fileIndex.Name) + "\n");

            foreach (var dependency in dependenciesWithSourceFile)
            {
                string relativePath = Path.GetRelativePath(path, dependency.Path);
                writer.Write("\n" + Path.ChangeExtension(dependency.Name, ".o") + ": " + Path.ChangeExtension(relativePath, ".cpp") + "\n\tg++ -c $<\n");
            }
        }
    }

    public List<string> Build(string rootSourceFileName)
    {
        List<string> commands = [];

        var rootSourceFileExtension = Path.GetExtension(rootSourceFileName);

        if (rootSourceFileExtension != ".cpp" && rootSourceFileExtension != ".c")
        {
            Console.WriteLine("The root file to compile from should be a source file: " + rootSourceFileName);
            Environment.Exit(0);
        }

        if (indexTable.TryGet(rootSourceFileName, out var fileIndex))
        {
            GetAllDependencies(fileIndex!);
            List<FileIndex> dependenciesWithSourceFile = []; // we only want to compile source files into object files

            foreach (var dependency in _dependencies)
            {
                if (indexTable.TryGet(Path.ChangeExtension(dependency.Name, ".cpp"), out var dependencySource))
                {
                    if (!indexTable.TryGet(Path.ChangeExtension(dependency.Name, ".o"), out var objectFile) || 
                        (objectFile!.Modified < dependency.Modified) || 
                        (objectFile.Modified < dependencySource!.Modified)) 
                    {
                        commands.Add(BuildCompileObjectFileCommand(dependencySource!));
                    }
                    
                    dependenciesWithSourceFile.Add(dependency);
                }
            }

            commands.Add(BuildCompileExecutableFileCommand(fileIndex!, dependenciesWithSourceFile));
        }

        return commands;
    }

    private static string BuildCompileObjectFileCommand(FileIndex fileIndex)
    {
        var path = Path.GetDirectoryName(fileIndex.Path);

        if (path == null) return string.Empty;

        return $"/C cd {path}\\ && g++ -c {fileIndex.Name}";
    }

    private static string BuildCompileExecutableFileCommand(FileIndex fileIndex, IEnumerable<FileIndex> dependencies)
    {
        var path = Path.GetDirectoryName(fileIndex.Path);

        if (path == null) return string.Empty;

        string command = $"/C cd {path}\\ && g++ {fileIndex.Name} ";

        foreach (var dependency in dependencies)
        {
            string relativePath = Path.GetRelativePath(path, dependency.Path);
            command += $"{Path.ChangeExtension(relativePath, ".o")} ";
        }

        return command + $"-o {Path.GetFileNameWithoutExtension(fileIndex.Name)}";
    }

    private void GetAllDependencies(FileIndex fileIndex)
    {
        var includes = GetIncludesFromFile(fileIndex);

        foreach (var dependency in includes)
        {
            var dependencyFileName = Path.GetFileName(dependency);

            // gets the dependencies that are in the header file
            if (indexTable.TryGet(dependencyFileName, out var dependencyFileIndex))
            {
                if (_dependencies.Contains(dependencyFileIndex!)) // breaks circular dependencies 
                    continue; 

                // get the dependencies that are in this header file and include this header file as a dependency as well
                _dependencies.Add(dependencyFileIndex!);

                GetAllDependencies(dependencyFileIndex!);

                // gets the dependenceies that are in the header file's corrosponding source file if it has one
                if (indexTable.TryGet(Path.ChangeExtension(dependencyFileName, ".cpp"), out var dependencySourceFileIndex))
                {
                    GetAllDependencies(dependencySourceFileIndex!);
                }
            }
        }
    }

    private static List<string> GetIncludesFromFile(FileIndex fileIndex)
    {
        List<string> includes = [];

        var fileInfo = new FileInfo(fileIndex.Path);

        using var fileStream = fileInfo.OpenRead();
        using var reader = new StreamReader(fileStream);
    
        while (!reader.EndOfStream)
        {
            string? line = reader.ReadLine();

            if (line == null)
                break;

            var matches = IncludeRegex().Matches(line);

            foreach (Match match in matches)
            {
                var path = match.Groups["path"].Value;

                if (path == null)
                    continue;

                includes.Add(path);
            }
        }

        return includes;
    }
}