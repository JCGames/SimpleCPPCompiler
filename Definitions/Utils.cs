namespace JCGames.SimpleCppCompiler;

internal class Utils
{
    public static void Clean()
    {
        CleanDirectory(Directory.GetCurrentDirectory());
    }

    private static void CleanDirectory(string directory)
    {
        foreach (var filePath in Directory.GetFiles(directory))
        {
            if (Path.GetExtension(filePath) == ".o") 
            {
                File.Delete(filePath);
            }
        }

        foreach (var dir in Directory.GetDirectories(directory))
        {
            CleanDirectory(dir);
        }
    }

    /// <summary>
    /// Executes all commands
    /// </summary>
    /// <param name="commands"></param>
    public static void ExecuteCommands(IEnumerable<string> commands)
    {
        var process = new System.Diagnostics.Process()
        {
            StartInfo = new()
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
            }
        };

        foreach (var command in commands)
        {
            Console.WriteLine(command);
            process.StartInfo.Arguments = command;
            process.Start();
            process.WaitForExit();
        }
    }
}