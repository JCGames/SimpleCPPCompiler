if (args.Length == 0)
    return;

var fileTable = new FileTable();
fileTable.CreateTable();

if (args[0] == "showd")
{
    fileTable.ShowDependencies();
    return;
}

var compiler = new Compiler(fileTable);

if (args[0] == "clean")
{
    compiler.CleanFiles();
    return;
}

compiler.Compile(args[0]);