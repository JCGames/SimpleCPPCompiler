using System.Text.RegularExpressions;

namespace JCGames.SimpleCppCompiler;

public static partial class HelpfulRegex 
{
    [GeneratedRegex("#include.*\"(?<path>.*)\"")]
    public static partial Regex HeaderFileIncludesRegex();
}