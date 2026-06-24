namespace IsDccSharp.Core;

using System;

public sealed class DecodeOptions
{
    public bool AutoUnscramble { get; set; } = true;
    public int IndentWidth { get; set; } = 6;
    public bool IncludeStringUserVariables { get; set; }
    public bool IncludeNumberUserVariables { get; set; }
    public bool IncludeDataTypes { get; set; }
    public bool IncludeFunctionPrototypes { get; set; }
    public bool IncludeFunctionDefinitions { get; set; } = true;

    internal void Validate()
    {
        if (IndentWidth < 2)
            throw new ArgumentOutOfRangeException(nameof(IndentWidth), "Indent width must be 2 or greater.");
    }
}
