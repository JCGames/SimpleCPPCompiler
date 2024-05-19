using System.Collections;

internal class FileIndexTable : IEnumerable<KeyValuePair<string, FileIndex>>
{
    private readonly Dictionary<string, FileIndex> _fileIndexTable = [];

    public FileIndexTable() => IndexFiles(Directory.GetCurrentDirectory());

    public FileIndexTable(string directory) => IndexFiles(directory);

    public bool ContainsKey(string key) => _fileIndexTable.ContainsKey(key);

    public bool TryGet(string key, out FileIndex? value) => _fileIndexTable.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, FileIndex>> GetEnumerator()
    {
        return _fileIndexTable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _fileIndexTable.GetEnumerator();
    }

    private void IndexFiles(string directory)
    {
        foreach (var filePath in Directory.GetFiles(directory))
        {
            var fileInfo = new FileInfo(filePath);
            var ext = Path.GetExtension(filePath);

            if (ext == ".cpp" || ext == ".c" || ext == ".hpp" || ext == ".h")
            {
                var fileIndex = new FileIndex 
                { 
                    Name = fileInfo.Name,
                    Path = filePath,
                    Modified = fileInfo.LastWriteTime
                };

                if (!_fileIndexTable.TryAdd(Path.GetFileName(filePath), fileIndex))
                {
                    Console.WriteLine("Cannot have duplicate file names in project: " + filePath);
                    Environment.Exit(1);
                }
            }
        }

        foreach (var dir in Directory.GetDirectories(directory))
            IndexFiles(dir);
    }
}