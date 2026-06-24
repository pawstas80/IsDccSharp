namespace IsDccSharp.Core;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class DecodeResult
{
    public DecodeResult(
        string text,
        string? sourcePath,
        long inputLength,
        bool wasUnscrambled,
        bool hasDecodedHeader,
        string infoString,
        IEnumerable<string>? warnings)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        SourcePath = sourcePath;
        InputLength = inputLength;
        WasUnscrambled = wasUnscrambled;
        HasDecodedHeader = hasDecodedHeader;
        InfoString = infoString ?? string.Empty;
        Warnings = (warnings ?? []).ToArray();
    }

    public string Text { get; }
    public string? SourcePath { get; }
    public long InputLength { get; }
    public bool WasUnscrambled { get; }
    public bool HasDecodedHeader { get; }
    public string InfoString { get; }
    public IReadOnlyList<string> Warnings { get; }
}
