namespace IsDccSharp.Core;

using System;
using System.Collections.Generic;

internal delegate void DecodeFunc(BinaryReaderHelper r, int opcode, int inCounter, ISData isData);

internal sealed class DecodeEntry(string text, int opNum, DecodeFunc? func)
{
    public string Text { get; } = text;
    public int OperationNumber { get; } = opNum;
    public DecodeFunc? DecodeFunction { get; } = func;

    public override string ToString() => $"{Text} : {DecodeFunction}";
}

internal static class DecodeTable
{
    private static readonly Lazy<IReadOnlyDictionary<int, DecodeEntry>> LazyTable = new(BuildTable);

    public static IReadOnlyDictionary<int, DecodeEntry> Table => LazyTable.Value;

    private static IReadOnlyDictionary<int, DecodeEntry> BuildTable()
    {
        var table = new Dictionary<int, DecodeEntry>();

        void Add(int opcode, string text, int op, DecodeFunc? f) => table[opcode] = new DecodeEntry(text, op, f);

        // Core control and arithmetic operations
        Add(0x0000, "??", OperationType.Unknown1, DecodeOps.DecodeOp);
        Add(0x0001, "goto", OperationType.Unknown, DecodeOps.DoGoto);
        Add(0x0002, "abort", OperationType.Unknown, DecodeOps.DoAbort);
        Add(0x0003, "exit", OperationType.Unknown, DecodeOps.DoExit);
        Add(0x0004, "if", OperationType.If, DecodeOps.DecodeOp);
        Add(0x0005, "goto2", OperationType.Unknown, DecodeOps.DoGoto2);

        Add(0x0006, "=", OperationType.Equate, DecodeOps.Equate);
        Add(0x0007, "+", OperationType.Plus, DecodeOps.DecodeOp);
        Add(0x0008, "%", OperationType.Mod, DecodeOps.DecodeOp);
        Add(0x0009, "<", OperationType.LessThan, DecodeOps.DecodeOp);
        Add(0x000A, ">", OperationType.GreaterThan, DecodeOps.DecodeOp);
        Add(0x000B, "<=", OperationType.LessThanEqual, DecodeOps.DecodeOp);
        Add(0x000C, ">=", OperationType.GreaterThanEqual, DecodeOps.DecodeOp);
        Add(0x000D, "==", OperationType.Equal, DecodeOps.DecodeOp);
        Add(0x000E, "!=", OperationType.NotEqual, DecodeOps.DecodeOp);
        Add(0x000F, "-", OperationType.Minus, DecodeOps.DecodeOp);
        Add(0x0010, "*", OperationType.Mult, DecodeOps.DecodeOp);
        Add(0x0011, "/", OperationType.Div, DecodeOps.DecodeOp);

        // Bitwise and logical operations
        Add(0x0012, "&", OperationType.BitAnd, DecodeOps.DecodeOp);
        Add(0x0013, "|", OperationType.BitOr, DecodeOps.DecodeOp);
        Add(0x0014, "^", OperationType.BitEor, DecodeOps.DecodeOp);
        Add(0x0015, "~", OperationType.BitNot, DecodeOps.DecodeOp);
        Add(0x0016, "<<", OperationType.ShiftL, DecodeOps.DecodeOp);
        Add(0x0017, ">>", OperationType.ShiftR, DecodeOps.DecodeOp);
        Add(0x0018, "||", OperationType.LogicOr, DecodeOps.DecodeOp);
        Add(0x0019, "&&", OperationType.LogicAnd, DecodeOps.DecodeOp);
        // Pointers, indirection, and addressing
        Add(0x001A, "&", OperationType.AddressOf, DecodeOps.DecodeOp);
        Add(0x001B, "*", OperationType.Indirection, DecodeOps.DecodeOp);
        Add(0x001C, "->", OperationType.Pointer, DecodeOps.DecodeOp);
        Add(0x001D, "[]", OperationType.StrBw, DecodeOps.DecodeOp);
        Add(0x001E, "[]", OperationType.StrBr, DecodeOps.DecodeOp);

        // Function calls and control flow
        Add(0x0020, "CallDllFx", OperationType.Unknown, DecodeOps.DoCall);
        Add(0x0021, "call", OperationType.Unknown, DecodeOps.DoCall);
        Add(0x0022, "functionStart", OperationType.Unknown, DecodeOps.FunctionStart);
        Add(0x0023, "return", OperationType.Unknown, DecodeOps.FuncReturn);
        Add(0x0024, "return", OperationType.Unknown, DecodeOps.FuncReturn);
        Add(0x0025, "return", OperationType.Unknown, DecodeOps.FuncReturn);
        Add(0x0026, "functionEnd", OperationType.Unknown, DecodeOps.FunctionEnd);
        Add(0x0027, "return", OperationType.Unknown, DecodeOps.FuncReturn);

        // String and memory operations
        Add(0x0028, "StrLengthChars", OperationType.StrLc, DecodeOps.DecodeOp);
        Add(0x0029, "StrSub", OperationType.StrSub, DecodeOps.DecodeOp);
        Add(0x002A, "StrFind", OperationType.StrFind, DecodeOps.DecodeOp);
        Add(0x002B, "StrCompare", OperationType.StrComp, DecodeOps.DecodeOp);
        Add(0x002C, "StrToNum", OperationType.StrToNum, DecodeOps.DecodeOp);
        Add(0x002D, "NumToStr", OperationType.NumToStr, DecodeOps.DecodeOp);

        // Exception and handler constructs
        Add(0x002F, "Handler", OperationType.Handler, DecodeOps.DecodeOp);
        Add(0x0030, "ExecuteHandler", OperationType.ExeHandler, DecodeOps.DecodeOp);
        Add(0x0031, "Resize", OperationType.Resize, DecodeOps.DecodeOp);
        Add(0x0032, "SizeOf", OperationType.SizeOf, DecodeOps.DecodeOp);

        // Structures and member access
        Add(0x0033, ".", OperationType.StructMember, DecodeOps.DecodeOp);
        Add(0x0034, "set", OperationType.Equate, DecodeOps.DecodeOp);
        Add(0x0035, "read", OperationType.StructMember1, DecodeOps.DecodeOp);

        // Flow constructs (try/catch/finally)
        Add(0x0036, "try", OperationType.Try, DecodeOps.DecodeOp);
        Add(0x0037, "catch", OperationType.Catch, DecodeOps.DecodeOp);
        Add(0x0038, "endcatch", OperationType.EndCatch, DecodeOps.DecodeOp);

        // DLL linking / resource handling
        Add(0x0039, "UseDll", OperationType.UseDll, DecodeOps.DecodeOp);
        Add(0x003A, "UnUseDll", OperationType.UnUseDll, DecodeOps.DecodeOp);

        // Miscellaneous / special operations
        Add(0x003B, "AskOptions", OperationType.Special1, DecodeOps.DecodeOp);
        Add(0x003C, "SPrintf", OperationType.Unknown2, DecodeOps.DecodeOp);
        Add(0x003D, "SPrintfBox", OperationType.Unknown2, DecodeOps.DecodeOp);

        // These opcodes are currently emitted as unknown operations.
        for (var i = 0x003E; i <= 0x0062; i++)
            Add(i, "??", OperationType.Unknown2, DecodeOps.DecodeOp);

        // Terminator opcode
        Add(0x0129, "EndCodeSegment", OperationType.Unknown, null);

        return table;
    }
}
