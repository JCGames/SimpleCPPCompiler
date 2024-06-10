namespace JCGames.SimpleCppCompiler;

public class FileDependencyTree 
{
    public FileNode? Root { get; private set; }

    /// <summary>
    /// Creates a file dependency tree with branches to every file's dependencies from the root file.
    /// </summary>
    public void CreateTree(FileTable fileTable, string rootFileName)
    {
        // the root file must be a .cpp file
        if (Path.GetExtension(rootFileName) is not ".cpp")
        {
            Console.WriteLine("Root file should be a source file.");
            Environment.Exit(1);
        }

        // if the root file name is found in the current directory
        if (fileTable.TryGet(rootFileName, out var fileInfo))
        {
            Root = new SourceFileNode(fileInfo!);
            var visited = new HashSet<string>();

            InternalCreateTree(Root, fileTable, visited);
        }
    }

    /// <returns>A list of source file dependencies.</returns>
    public List<MyFileInfo> GetDependenciesList()
    {
        if (Root is null) return [];

        var dependenciesHashSet = new HashSet<MyFileInfo>();
        InternalGetDependenciesList(Root, dependenciesHashSet);

        if (dependenciesHashSet.Contains(Root.FileInfo))
            dependenciesHashSet.Remove(Root.FileInfo);

        return [.. dependenciesHashSet];
    }

    /// <summary>
    /// Prints the file dependency tree.
    /// </summary>
    public void PrintTree()
    {
        if (Root is not null)
            InternalPrintTree(Root);   
    }

    private static void InternalGetDependenciesList(FileNode currentNode, HashSet<MyFileInfo> dependenciesHashSet)
    {
        if (currentNode is SourceFileNode sourceFileNode)
            dependenciesHashSet.Add(sourceFileNode.FileInfo);

        foreach (var child in currentNode.Children)
            InternalGetDependenciesList(child, dependenciesHashSet);
    }

    private static void InternalCreateTree(FileNode root, FileTable fileTable, HashSet<string> visited)
    {
        if (visited.Contains(root.FileInfo.Name))
            return;

        visited.Add(root.FileInfo.Name);

        if (Path.GetExtension(root.FileInfo!.Path) is ".hpp" or ".h")
        {
            foreach (var link in root.FileInfo.Links)
            {
                var sourceFile = new SourceFileNode(link);
                root.Children.Add(sourceFile);
                InternalCreateTree(sourceFile, fileTable, visited);
            }
        }

        root.FileInfo.ForEachIncludeInFileStream(path =>
        {
            if (fileTable.TryGet(Path.GetFileName(path), out var fileInfo))
            {
                var headerFile = new HeaderFileNode(fileInfo!);
                root.Children.Add(headerFile);
                InternalCreateTree(headerFile, fileTable, visited);
            }
        });
    }

    private static void InternalPrintTree(FileNode root, string indent = "")
    {
        Console.WriteLine(indent + root.FileInfo.Name);

        foreach (var child in root.Children)
            InternalPrintTree(child, indent + '\t');
    }
}