namespace JCGames.SimpleCppCompiler;

public class FileNode(MyFileInfo fileInfo)
{
    public MyFileInfo FileInfo { get; set; } = fileInfo;
    public List<FileNode> Children { get; set; } = [];
}