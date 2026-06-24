namespace IsDccSharp.Core;

internal static class OperationType
{
    public const int Unknown = 0;
    public const int Equate = 1;
    public const int LessThan = 2;
    public const int GreaterThan = 3;
    public const int LessThanEqual = 4;
    public const int GreaterThanEqual = 5;
    public const int Equal = 6;
    public const int NotEqual = 7;
    public const int Plus = 8;
    public const int Minus = 9;
    public const int Mult = 10;
    public const int Div = 11;
    public const int BitAnd = 12;
    public const int BitOr = 13;
    public const int BitEor = 14;
    public const int BitNot = 15;
    public const int ShiftL = 16;
    public const int ShiftR = 17;
    public const int LogicAnd = 18;
    public const int LogicOr = 19;
    public const int StrCat = 20;
    public const int PathCat = 21;
    public const int Mod = 22;
    public const int LogicNot = 23;
    public const int StructMember = 24;
    public const int Pointer = 25;
    public const int Indirection = 26;
    public const int AddressOf = 27;
    public const int If = 28;
    public const int Unknown1 = 29;
    public const int Unknown2 = 30;
    public const int Try = 31;
    public const int Catch = 32;
    public const int EndCatch = 33;
    public const int StructMember1 = 34;
    public const int StructMember2 = 35;
    public const int UseDll = 36;
    public const int UnUseDll = 37;
    public const int StrToNum = 38;
    public const int NumToStr = 39;
    public const int StrComp = 40;
    public const int StrFind = 41;
    public const int StrSub = 42;
    public const int StrLc = 43;
    public const int StrBr = 44;
    public const int StrBw = 45;
    public const int Resize = 46;
    public const int SizeOf = 47;
    public const int ExeHandler = 48;
    public const int Handler = 49;
    public const int Special1 = 50;
}

internal static class Operations
{
    public static readonly string?[] Names =
    [
        null, "=", "<", ">", "<=", ">=", "==", "!=",
        "+", "-", "*", "/", "&", "|", "^", "~", "<<",
        ">>", "&&", "||", "+", "^", "%", "!", ".", "->", "*", "&", "if", "??", "??",
        "try", "catch", "endcatch", "", ".", "UseDll", "UnUseDll", "StrToNum",
        "NumToStr", "StrCompare", "StrFind", "StrSub", "StrLengthChars", "", "",
        "Resize", "SizeOf", "ExecuteHandler", "Handler", "Set2Handler??"
    ];
}
