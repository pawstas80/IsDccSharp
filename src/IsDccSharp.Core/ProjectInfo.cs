namespace IsDccSharp.Core;

using System;
using System.Collections.Generic;
using System.Text;

public static class ProjectInfo
{
    public const string AppName = "IsDccSharp";
    public const string Version = "4.0.0";
    public const string Author = "pawstas80";
    public const string UpstreamProjectUrl = "https://github.com/darknesswind/IsDcc";

    public static IReadOnlyList<string> OriginalCredits { get; } = Array.AsReadOnly(
    [
        "isDcc v1.x, (c) 1998 Andrew de Quincey",
        "isDcc v2.00, (c) 2000 Mr. Smith",
        "isDcc v2.10, (c) 2001 Mr. Smith",
        "isDcc v3.10, (c) 2001 Mr Won't tell :)",
        "isDcc v4.00, (c) 2021 pawstas80"
    ]);

    public static IReadOnlyList<string> ImprovementNotes { get; } = Array.AsReadOnly(
    [
        "C# port for .NET Framework 4.8",
        "automatic aLuZ detection to avoid double-unscrambling",
        "goto2 alignment fix for newer INX files",
        "TYPE_UNDEF5 correction for type 13",
        "Stirling Technologies info string support as a warning"
    ]);

    public static IEnumerable<string> GetConsoleHeaderLines()
    {
        yield return $"{AppName} {Version}";
        yield return "Original credits:";

        foreach (var credit in OriginalCredits)
            yield return "  " + credit;

        yield return $"Ported and fixed from: {UpstreamProjectUrl}";
        yield return "Improvements:";

        foreach (var note in ImprovementNotes)
            yield return "  - " + note;
    }

    public static string AddFileHeader(string decodedText)
    {
        if (decodedText == null)
            throw new ArgumentNullException(nameof(decodedText));

        var header = new StringBuilder();
        header.AppendLine($"// {AppName} {Version}");
        header.AppendLine("// Original credits:");

        foreach (var credit in OriginalCredits)
            header.AppendLine("// " + credit);

        header.AppendLine($"// Ported and fixed from: {UpstreamProjectUrl}");
        header.AppendLine("// Improvements:");

        foreach (var note in ImprovementNotes)
            header.AppendLine("// - " + note);

        header.AppendLine();
        header.Append(decodedText);
        return header.ToString();
    }
}
