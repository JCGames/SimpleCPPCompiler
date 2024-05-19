if (args.Length < 1) return;

var fileIndexTable = new FileIndexTable();

var commandsBuilder = new CommandsBuilder(fileIndexTable);
var commands = commandsBuilder.Build(args[0]);

CommandsExecuter.Execute(commands);