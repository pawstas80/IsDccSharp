namespace IsDccSharp.Core;

internal enum ParamType
{
    Unknown = 0,
    StringConst = 1,
    LongConst = 2,
    SystemStringVar = 3,
    UserStringVar = 4,
    SystemNumberVar = 5,
    UserNumberVar = 6,
    FnParamStringVar = 7,
    FnParamNumberVar = 8,
    FnLocalStringVar = 9,
    FnLocalNumberVar = 10,
    Label = 11,
    DataTypeNum = 12
}
internal enum OtherType
{
    Unknown = 0,
    UserFunction = 1,
    Label = 2,
    NumParams = 3
}
internal enum CodeLineType
{
    Function = 0,
    Operation = 1,
    IfStatement = 2,
    Comparison = 3,
    Label = 4,
    Goto = 5,
    Exit = 6,
    Abort = 7,
    FuncReturn = 8,
    Handler = 9,
    Call = 10,
    Return = 11,
    FuncEnd = 12
}
