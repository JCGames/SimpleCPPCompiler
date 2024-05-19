internal class CommandsExecuter
{
    /// <summary>
    /// Executes all commands
    /// </summary>
    /// <param name="commands"></param>
    public static void Execute(IEnumerable<string> commands)
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