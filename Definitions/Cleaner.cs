internal class Cleaner
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
}