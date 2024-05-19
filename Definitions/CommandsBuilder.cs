using System.Text.Json;
using System.Text.RegularExpressions;

internal partial class CommandsBuilder(FileIndexTable fileIndexTable)
{
    private readonly FileIndexTable indexTable = fileIndexTable;

    [GeneratedRegex("#include.*\"(?<path>.*)\"")]
    private static partial Regex IncludeRegex();

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
            var dependencies = GetAllDependencies(fileIndex!);

            foreach (var dependency in dependencies)
                Console.WriteLine(JsonSerializer.Serialize(dependency, new JsonSerializerOptions { WriteIndented = true }));

            foreach (var dependency in dependencies)
            {
                if (indexTable.TryGet(Path.ChangeExtension(dependency.Name, ".cpp"), out var dependencySource))
                {
                    commands.Add(BuildCompileObjectFileCommand(dependencySource!));
                }
            }

            commands.Add(BuildCompileExecutableFileCommand(fileIndex!, dependencies));
        }

        return commands;
    }

    private string BuildCompileObjectFileCommand(FileIndex fileIndex)
    {
        var path = Path.GetDirectoryName(fileIndex.Path);

        if (path == null) return string.Empty;

        return $"/C cd {path}\\ && g++ -c {fileIndex.Name}";
    }

    private string BuildCompileExecutableFileCommand(FileIndex fileIndex, IEnumerable<FileIndex> dependencies)
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

    private HashSet<FileIndex> GetAllDependencies(FileIndex fileIndex)
    {
        HashSet<FileIndex> results = [];

        var includes = GetIncludesFromFile(fileIndex);

        foreach (var dependency in includes)
        {
            var dependencyFileName = Path.GetFileName(dependency);

            // gets the dependencies that are in the header file
            if (indexTable.TryGet(dependencyFileName, out var dependencyFileIndex))
            {
                if (results.Contains(dependencyFileIndex!)) // breaks circular dependencies 
                    continue; 

                results.Add(dependencyFileIndex!);

                foreach (var dfi in GetAllDependencies(dependencyFileIndex!))
                    results.Add(dfi);

                // gets the dependenceies that are in the header file's corrosponding source file if it has one
                if (indexTable.TryGet(Path.ChangeExtension(dependencyFileName, ".cpp"), out var dependencySourceFileIndex))
                {
                    foreach (var dfi in GetAllDependencies(dependencySourceFileIndex!))
                        results.Add(dfi);
                }
            }
        }

        return results;
    }

    private List<string> GetIncludesFromFile(FileIndex fileIndex)
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