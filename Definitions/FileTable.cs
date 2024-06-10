using System.Collections;
using System.Text.Json;

namespace JCGames.SimpleCppCompiler;

public class FileTable : IEnumerable<KeyValuePair<string, MyFileInfo>>
{
    private readonly Dictionary<string, MyFileInfo> _fileTable = [];

    public FileTable()
    { 
        IndexFiles(Directory.GetCurrentDirectory());

        foreach (var (_, value) in _fileTable)
        {
            if (Path.GetExtension(value.Name) is ".hpp" or ".cpp" or ".h" or ".c")
                LinkSourceFileToItsHeaderFiles(value);
        }
    }

    #region Public Methods

    public FileTable(string directory) => IndexFiles(directory);

    public bool ContainsKey(string key) => _fileTable.ContainsKey(key);

    public bool TryGet(string key, out MyFileInfo? value) => _fileTable.TryGetValue(key, out value);

    public override string ToString()
    {
        var writer = new StringWriter();

        foreach (var (key, value) in _fileTable)
        {
            writer.WriteLine($"KEY: [{key}]");
            writer.WriteLine($"VALUE: {JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true })}");
            writer.WriteLine();
        }

        return writer.ToString();
    }

    public IEnumerator<KeyValuePair<string, MyFileInfo>> GetEnumerator()
    {
        return _fileTable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _fileTable.GetEnumerator();
    }

    #endregion

    #region Private Methods

    private void IndexFiles(string directory)
    {
        foreach (var filePath in Directory.GetFiles(directory))
        {
            var fileInfo = new FileInfo(filePath);
            var ext = Path.GetExtension(filePath);

            if (ext is ".cpp" or ".c" or ".hpp" or ".h" or ".exe" or ".o")
            {
                var fileIndex = new MyFileInfo(fileInfo);

                if (!_fileTable.TryAdd(Path.GetFileName(filePath), fileIndex) && (ext is not ".exe" and not ".o"))
                {
                    Console.WriteLine("Cannot have duplicate file names in project: " + filePath);
                    Environment.Exit(1);
                }
            }
        }

        foreach (var dir in Directory.GetDirectories(directory))
            IndexFiles(dir);
    }

    /// <summary>
    /// Method for linking header files to their source files.
    /// </summary>
    private void LinkSourceFileToItsHeaderFiles(MyFileInfo sourceFileInfo)
    {
        if (Path.GetExtension(sourceFileInfo.Name) is ".hpp" or ".h")
            return;

        sourceFileInfo.ForEachIncludeInFileStream(path => 
        {
            if (_fileTable.TryGetValue(Path.GetFileName(path), out var headerFile)) 
                    headerFile.Links.Add(sourceFileInfo);
        });
    }

    #endregion
}