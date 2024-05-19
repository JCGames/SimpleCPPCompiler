if (args.Length <= 0) return;

if (args[0] == "clean")
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
var fileIndexTable = new FileIndexTable();
var commandsBuilder = new CommandsBuilder(fileIndexTable);

if (createMakeFile)
    commandsBuilder.GenerateMakeFile(args[0]);
else
    CommandsExecuter.Execute(commandsBuilder.Build(args[0]));

totalTimeSW.Stop();
Console.WriteLine("Total execution time: " + totalTimeSW.Elapsed);