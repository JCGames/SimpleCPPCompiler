using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal class FileTable : IEnumerable<FilePointer>
{
    private readonly Dictionary<string, FilePointer> _fileTable = [];

    public FilePointer this[string filePath] => _fileTable[filePath];

    public bool Contains(string filePath)
    {
        return _fileTable.ContainsKey(filePath);
    }

    public void CreateTable()
    {
        var rootDirectory = Directory.GetCurrentDirectory();
        FindFilesAndAddToTable(rootDirectory);
    }

    public void FindFilesAndAddToTable(string directory)
    {
        var filePaths = Directory.GetFiles(directory);
        var workingDirectory = Directory.GetCurrentDirectory() + "\\";

        foreach (var filePath in filePaths)
        {
            var ext = Path.GetExtension(filePath);

            if (ext != ".cpp" && ext != ".hpp" && ext != ".o" && !(ext == ".exe" && filePath[0..(filePath.Length - Path.GetFileName(filePath).Length)] == workingDirectory))
                continue;

            var file = new FilePointer
            {
                Name = Path.GetFileName(filePath),
                Directory = directory + '\\',
            };

            var fileInfo = new FileInfo(filePath);
            using var fs = fileInfo.OpenRead();
            using var ss = new StreamReader(fs);

            while (!ss.EndOfStream)
            {
                var line = ss.ReadLine();

                var matches = Regex.Matches(line!, "#include.*\"(?<r>.*)\"");

                foreach (Match match in matches)
                {
                    var dependency = match.Groups["r"].Value
                        .Replace('/', '\\');
                        
                    var dependencyPath = Path.GetFullPath(directory + '\\' + dependency);

                    file.Dependencies.Add(dependencyPath);
                }
            }

            file.ModifiedDate = fileInfo.LastWriteTime;

            _fileTable.Add(filePath, file);
        }

        var directoryPaths = Directory.GetDirectories(directory);

        foreach (var dir in directoryPaths)
        {
            FindFilesAndAddToTable(dir!);
        }
    }

    public void ShowDependencies()
    {
        foreach (var file in _fileTable)
        {
            if (file.Value.Dependencies.Count == 0)
                continue;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(file.Key + " ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(file.Value.Name + " ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("depends on ->");
            Console.ResetColor();
            foreach (var dependency in file.Value.Dependencies)
            {
                Console.WriteLine("\t> " + dependency);
            }
        }
        Console.WriteLine();
    }

    public IEnumerator<FilePointer> GetEnumerator()
    {
        return _fileTable.Select(x => x.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _fileTable.Select(x => x.Value).GetEnumerator();
    }
}