namespace IsDccSharp.Core;

using System.Collections.Generic;
using System.Linq;

internal class Parameter
{
    public long ReadOffset { get; set; }
    public ParamType Type { get; set; } = ParamType.Unknown;
    public bool ByRef { get; set; }
    public int IntVal { get; set; }
    public string StringVal { get; set; } = "";
    public int VariableNumber { get; set; }
    public int UserFunction { get; set; }
    public int Label { get; set; }
    public override string ToString()
    {
        return Type switch
        {
            ParamType.StringConst => $"\"{StringVal}\"",
            ParamType.LongConst => IntVal.ToString(),
            ParamType.Label => $"Label_{IntVal}",
            _ => StringVal ?? $"var{VariableNumber}"
        };
    }
}

internal class CodeLine
{
    public CodeLineType Type { get; set; } = CodeLineType.Operation;
    public long Offset { get; set; }
    public int Opcode { get; set; }
    public string? Name { get; set; }
    public Parameter? Destination { get; set; }
    public List<Parameter> Params { get; set; } = [];
    public int ParamsCount { get; set; }
    public List<CodeLine> SubCodeLines { get; set; } = [];
    public int FunctionNumber { get; set; }
    public string StringOp { get; set; } = string.Empty;
    public int OperationNumber { get; set; }
    public int DestLabel { get; set; }
    public int ComparisonType { get; set; }
    public int LabelNumber { get; set; }
}

internal class Label
{
    public int Usage { get; set; }
    public int Position { get; set; }
    public int FilePosition { get; set; }
    public int FunctionNumber { get; set; }
    public int Passed { get; set; }
}

internal class FunctionBody
{
    public List<CodeLine> CodeLines { get; set; } = [];
    public int[]? LocalParamSize { get; set; }
    public int Prototype { get; set; }
    public int CodeLinesMax { get; set; }
}

internal class FunctionPrototype
{
    public long Offset { get; set; }
    public FunctionType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Label { get; set; }
    public int ReturnType { get; set; }
    public FunctionBody? FunctionBody { get; set; }
    public List<int> Params { get; set; } = [];
    public int ParamStringNamesCount { get; set; }
    public List<string> ParamStringNames { get; set; } = [];
    public int ParamNumberNamesCount { get; set; }
    public List<string> ParamNumberNames { get; set; } = [];
    public int[]? LocalParamSize { get; set; }
    public ushort ParamsCount { get; set; }

    public override string ToString()
    {
        return Name;
    }
}

internal enum VarType : uint
{
    fixstr = 0, Char = 1, Long = 2, Int = 3, number = 4, LIST = 5, BOOL = 6, HWND = 7,
    UNDEF1 = 8, CONSTANT = 9, UNDEF2 = 10, UNDEF3 = 11, UNDEF4 = 12, UNDEF5 = 13
}

internal class TypeEntry
{
    public long Offset { get; set; }
    public VarType Type { get; set; }
    public uint Size { get; set; }
    public string Name { get; set; } = string.Empty;
    public override string ToString()
    {
        return Name;
    }
}

internal class DataType
{
    public string Name { get; set; } = string.Empty;
    public List<TypeEntry> Entries { get; set; } = [];
    public override string ToString()
    {
        return $"{Name}:{string.Join(",", Entries.Select(x => x.Name).ToArray())}";
    }
}

internal class ISData
{
    public uint CRC { get; set; }
    public byte FileVersion { get; set; }
    public string InfoString { get; set; } = string.Empty;
    public byte CompilerVersion { get; set; }
    public long EOFPos { get; set; }
    public List<FunctionPrototype> FunctionPrototypes { get; set; } = [];
    public List<FunctionBody> FunctionBodies { get; set; } = [];
    public List<DataType> DataTypes { get; set; } = [];
    public List<CodeLine> CodeLines { get; set; } = [];
    public List<Label> Labels { get; set; } = [];
    public List<int> StringUserVars { get; set; } = [];
    public List<int> NumberUserVars { get; set; } = [];
    public List<string> StringSysVars { get; set; } = [];
    public List<string> NumberSysVars { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public int EndCodeSegmentOffset { get; set; }
    public int CodeLinesMax { get; set; }
}
