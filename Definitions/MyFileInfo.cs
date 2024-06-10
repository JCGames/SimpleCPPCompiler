using System.Text.RegularExpressions;

namespace JCGames.SimpleCppCompiler;

public class MyFileInfo(FileInfo fileInfo)
{
    private FileInfo _fileInfo = fileInfo;

    public string Name { get { return _fileInfo.Name; } }
    public string Path { get { return _fileInfo.FullName; } }
    public DateTime? Modified { get { return _fileInfo.LastWriteTime; } }
    public List<MyFileInfo> Links { get; set; } = [];

    public FileStream Open(FileMode mode) => _fileInfo.Open(mode);

    /// <returns>A list of includes as paths.</returns>
    public List<string> GetAllIncludesFromFileStream()
    {
        List<string> includes = [];
        using var reader = new StreamReader(_fileInfo.Open(FileMode.Open));

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            if (line is null)
                continue;

            // does line have one or more includes
            var matches = HelpfulRegex.HeaderFileIncludesRegex().Matches(line);

            foreach (Match match in matches)
                includes.Add(match.Groups["path"].Value);
        }

        return includes;
    }

    /// <summary>
    /// Finds all of the includes in a file and executes a given action every time one is found.
    /// </summary>
    public List<string> ForEachIncludeInFileStream(Action<string> action)
    {
        List<string> includes = [];
        using var reader = new StreamReader(_fileInfo.Open(FileMode.Open));

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            if (line is null)
                continue;

            // does line have one or more includes
            var matches = HelpfulRegex.HeaderFileIncludesRegex().Matches(line);

            foreach (Match match in matches)
                action?.Invoke(match.Groups["path"].Value);
        }

        return includes;
    }
}