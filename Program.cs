using JCGames.SimpleCppCompiler;
using System.Diagnostics;

if (args.Length <= 0) return;

var sw = Stopwatch.StartNew();

var fileTable = new FileTable();
var fileDependencyTree = new FileDependencyTree();
fileDependencyTree.CreateTree(fileTable, args[0]);

if (fileDependencyTree.Root is not null)
{
    var commands = CommandsBuilder.Build(fileDependencyTree.Root.FileInfo, fileDependencyTree.GetDependenciesList());
    Utils.ExecuteCommands(commands);
}

sw.Stop();
Console.WriteLine("Total execution time took: " + sw.Elapsed);