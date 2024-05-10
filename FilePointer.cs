internal class FilePointer
{
    public string Name { get; set; } = string.Empty;
    public string Directory { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = [];
    public DateTime ModifiedDate { get; set; } = DateTime.MinValue;
    public bool ShouldBeCompiled { get; set; } = false;
    public bool HaveDependenciesBeenTouched { get; set; } = false;
}