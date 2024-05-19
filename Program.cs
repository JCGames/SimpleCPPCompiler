if (args.Length <= 0) return;

var fileIndexTable = new FileIndexTable();

if (args[0] == "clean" && args.Length > 1)
{
    Cleaner.Clean();
    return;   
}

bool createMakeFile = false;

foreach (var arg in args)
{
    if (arg == "--makefile")
    {
        createMakeFile = true;
    }
}

var totalTimeSW = System.Diagnostics.Stopwatch.StartNew();
var commandsBuilder = new CommandsBuilder(fileIndexTable);

if (createMakeFile)
    commandsBuilder.GenerateMakeFile(args[0]);
else
    CommandsExecuter.Execute(commandsBuilder.Build(args[0]));

totalTimeSW.Stop();
Console.WriteLine("Total execution time: " + totalTimeSW.Elapsed);