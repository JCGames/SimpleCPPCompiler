namespace JCGames.SimpleCppCompiler;

internal static class CommandsBuilder
{
    public static List<string> Build(MyFileInfo root, List<MyFileInfo> dependencies)
    {
        var commands = new List<string>();

        foreach (var dependency in dependencies)
            commands.Add(BuildCompileObjectFileCommand(dependency));

        commands.Add(BuildCompileExecutableFileCommand(root, dependencies));

        return commands;
    }

    private static string BuildCompileObjectFileCommand(MyFileInfo fileIndex)
    {
        var path = Path.GetDirectoryName(fileIndex.Path);

        if (path == null) return string.Empty;

        return $"/C cd {path}\\ && g++ -c {fileIndex.Name}";
    }

    private static string BuildCompileExecutableFileCommand(MyFileInfo fileIndex, IEnumerable<MyFileInfo> dependencies)
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
}