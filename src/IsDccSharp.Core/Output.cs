namespace IsDccSharp.Core;

using System;
using System.IO;
using System.Linq;
using System.Text;
internal static class Output
{
    private static readonly string[] TypeNames =
    [
        "fixstr", "char", "long", "int", "number",
        "LIST", "BOOL", "HWND", "UNDEF1", "CONSTANT", "UNDEF2",
        "UNDEF3", "UNDEF4", "UNDEF5"
    ];

    public static void Generate(
        ISData isData,
        TextWriter w,
        int indentWidth = 4,
        bool stringUserVars = false,
        bool numberUserVars = false,
        bool dataTypes = false,
        bool functionPrototypes = false,
        bool functionDefinitions = true)
    {
        w.Write("declare\n");
        if (stringUserVars)
        {
            if (isData.StringUserVars.Any())
            {
                w.Write(" ".PadLeft(indentWidth - 2));
                w.Write("// ------------- STRING VARIABLES --------------\n");
            }

            for (var i = 0; i < isData.StringUserVars.Count; i++)
            {
                w.Write(" ".PadLeft(indentWidth));
                w.WriteLine($"string string{i};");
            }
        }

        if (numberUserVars)
        {

            if (isData.NumberUserVars.Any())
            {
                w.WriteLine();
                w.Write(" ".PadLeft(indentWidth));
                w.Write("// ------------- NUMBER VARIABLES --------------\n");
            }

            for (var i = 0; i < isData.NumberUserVars.Count; i++)
            {
                w.Write(" ".PadLeft(indentWidth));
                w.WriteLine($"number number{i};");
            }

            if (isData.NumberUserVars.Any())
                w.WriteLine();
        }

        if (dataTypes)
        {
            if (isData.DataTypes.Any())
            {
                w.Write(" ".PadLeft(indentWidth - 2));
                w.Write("// ------------- DATA TYPES --------------\n");
            }

            for (var i = 0; i < isData.DataTypes.Count; i++)
            {
                w.Write(" ".PadLeft(indentWidth - 2));
                w.Write(
                    $"typedef {(string.IsNullOrEmpty(isData.DataTypes[i].Name) ? "(null)" : $"{isData.DataTypes[i].Name}")}\n");
                w.Write(" ".PadLeft(indentWidth - 2));
                w.WriteLine("begin");

                for (var j = 0; j < isData.DataTypes[i].Entries.Count; j++)
                {
                    w.Write(" ".PadLeft(indentWidth + 2));
                    switch (isData.DataTypes[i].Entries[j].Type)
                    {
                        case VarType.Long:
                            w.Write("long ");
                            break;
                        case VarType.Int:
                            w.Write("int ");
                            break;
                        case VarType.Char:
                            w.Write("char ");
                            break;
                        default:
                            w.Write($"{isData.DataTypes[i].Entries[j].Type} ");
                            break;
                    }

                    w.Write($"{isData.DataTypes[i].Entries[j].Name}");

                    if (isData.DataTypes[i].Entries[j].Type == 0)
                        w.Write($"[{isData.DataTypes[i].Entries[j].Size}]");
                    w.Write(";\n");
                }

                w.Write(" ".PadLeft(indentWidth - 2));
                w.Write("end;\n");

                if (i < (isData.DataTypes.Count - 1))
                    w.Write("\n");
            }

            if (isData.DataTypes.Any())
                w.Write("\n");
        }

        if (functionPrototypes)
        {
            if (isData.FunctionPrototypes.Any())
            {
                w.Write(" ".PadLeft(indentWidth - 2));
                w.Write("// ------------- FUNCTION PROTOTYPES --------------\n");
            }

            for (var i = 0; i < isData.FunctionPrototypes.Count; i++)
            {
                if ((isData.FunctionPrototypes[i].FunctionBody == null) &&
                    (isData.FunctionPrototypes[i].Type == FunctionType.User))
                    continue;

                w.Write(" ".PadLeft(indentWidth - 2));
                w.Write($"{i} prototype {isData.FunctionPrototypes[i].Name}(");

                for (var j = 0; j < isData.FunctionPrototypes[i].ParamsCount; j++)
                {
                    if ((isData.FunctionPrototypes[i].Params[j] & 0x80000000) != 0) w.Write("BYREF ");

                    w.Write($"{TypeNames[isData.FunctionPrototypes[i].Params[j] & 0xffffff]}");

                    if (j < (isData.FunctionPrototypes[i].ParamsCount - 1))
                        w.Write(", ");
                }

                w.Write("); ");
                if (isData.FunctionPrototypes[i].Label != 0xFFFF)
                    w.Write($"starting at {isData.FunctionPrototypes[i].Label:X6}");
                w.Write("\n");
            }

            if (isData.FunctionPrototypes.Any())
                w.Write("\n");
        }

        if (functionDefinitions)
        {
            w.Write(" ".PadLeft(indentWidth - 2));
            w.WriteLine("// ------------- FUNCTION DEFS --------------");
            for (var i = 0; i < isData.FunctionPrototypes.Count; i++)
            {
                var fp = isData.FunctionPrototypes[i];

                if (fp.Type == FunctionType.Dll) continue;
                if (fp.FunctionBody == null) continue;

                w.Write(" ".PadLeft(indentWidth - 2));
                w.WriteLine($"// ------------- FUNCTION {fp.Name} ({i}) --------------");

                w.Write(" ".PadLeft(indentWidth - 2));
                w.Write($"function {fp.Name}(");
                var strParam = 0;
                var numParam = 0;
                for (var j = 0; j < fp.ParamsCount; j++)
                {
                    switch ((VarType)(fp.Params[j] & 0xFFFFFF))
                    {
                        case VarType.fixstr:
                            w.Write(fp.ParamStringNames[strParam++]);
                            break;
                        default:
                            w.Write(fp.ParamNumberNames[numParam++]);
                            break;
                    }

                    if (j < fp.ParamsCount - 1) w.Write(", ");
                }

                w.WriteLine(")");

                w.Write(" ".PadLeft(indentWidth - 2));
                w.WriteLine("begin");
                var body = fp.FunctionBody;
                for (var j = 0; j < body.CodeLines.Count; j++)
                {
                    var line = body.CodeLines[j];
                    if (line == null) continue;
                    PrintLine(isData, line, w, indentWidth + 1, i);
                }

                if (body.CodeLines.Count > 0 && body.CodeLines[body.CodeLines.Count - 1]?.Type == CodeLineType.Label)
                {
                    PrintIndent(w, indentWidth - 1);
                    w.WriteLine("return;");
                }

                w.Write(" ".PadLeft(indentWidth - 2));
                w.WriteLine("end;");
                if (i < isData.FunctionPrototypes.Count - 1)
                {
                    w.WriteLine();
                    w.WriteLine();
                }
            }
        }
    }


    private static void PrintArgs(ISData isData, CodeLine c, TextWriter w, int function)
    {
        for (var i = 0; i < c.Params.Count; i++)
        {
            PrintArg(isData, c.Params[i], w, function);
            if (i < c.Params.Count - 1) w.Write(", ");
        }
    }

    private static void PrintArg(ISData isData, Parameter p, TextWriter w, int function)
    {
        switch (p.Type)
        {
            case ParamType.SystemStringVar:
                w.Write(isData.StringSysVars[p.VariableNumber] /*.Name*/);
                break;

            case ParamType.UserStringVar:
                w.Write($"string{p.VariableNumber}");
                break;

            case ParamType.FnParamStringVar:
                if (function == -1) throw new InvalidOperationException("Unexpected function parameter outside a function.");
                if (p.VariableNumber >= isData.FunctionPrototypes[function].ParamStringNamesCount)
                    throw new InvalidOperationException("Invalid string parameter index.");
                w.Write(isData.FunctionPrototypes[function].ParamStringNames[p.VariableNumber]);
                break;

            case ParamType.FnLocalStringVar:
                if (function == -1) throw new InvalidOperationException("Unexpected function local outside a function.");
                w.Write($"lString{p.VariableNumber}");
                break;

            case ParamType.SystemNumberVar:
                if (isData.NumberSysVars.Count != 0)
                    w.Write(isData.NumberSysVars[p.VariableNumber] /*.Name*/);
                else
                    w.Write(p.VariableNumber);
                break;

            case ParamType.UserNumberVar:
                w.Write($"number{p.VariableNumber}");
                break;

            case ParamType.FnParamNumberVar:
                if (function == -1) throw new InvalidOperationException("Unexpected function parameter outside a function.");
                if (p.VariableNumber >= isData.FunctionPrototypes[function].ParamNumberNames.Count)
                    throw new InvalidOperationException("Invalid number parameter index.");
                w.Write(isData.FunctionPrototypes[function].ParamNumberNames[p.VariableNumber]);
                break;

            case ParamType.FnLocalNumberVar:
                if (function == -1) throw new InvalidOperationException("Unexpected function local outside a function.");
                w.Write($"lNumber{p.VariableNumber}");
                break;

            case ParamType.StringConst:
                w.Write($"\"{EscapeStringLiteral(p.StringVal)}\"");
                break;

            case ParamType.LongConst:
                if (p.IntVal < 256) w.Write(p.IntVal);
                else w.Write($"0x{p.IntVal:x}");
                break;

            case ParamType.DataTypeNum:
                w.Write(isData.DataTypes[p.IntVal].Name);
                if (string.IsNullOrEmpty(isData.DataTypes[p.IntVal].Name))
                    w.Write($",{p.IntVal}");
                break;

            default:
                throw new InvalidOperationException($"Unsupported parameter type {p.Type}.");
        }
    }

    private static string EscapeStringLiteral(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\'': sb.Append("\\'"); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                default:
                    if ((c > 0x00 && c < 0x20) || c >= 0x7F)
                        sb.Append($"\\x{(int)c:X2}");
                    else
                        sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static void PrintLine(ISData isData, CodeLine code, TextWriter w, int indent, int function)
    {
        var previous_label = 0;
        for (var i = 0; i < isData.Labels.Count; i++)
        {
            var L = isData.Labels[i];
            if (L.FilePosition < code.Offset && L.Passed == 1)
            {
                previous_label = i;
            }

            if (L.FilePosition < code.Offset && L.Passed == 0)
            {
                previous_label = i;
                L.Passed = 1;
                w.WriteLine($"Label{i}:");
            }

            if (L.FilePosition == code.Offset)
            {
                previous_label = i;
                L.Passed = 1;
                w.WriteLine($"Label{i}:");
                return;
            }

            if (L.FilePosition >= code.Offset) break;
        }

        if (code.Type != CodeLineType.Label)
            w.Write($"{code.Offset:X6}:{code.Opcode:X4}: ");

        switch (code.Type)
        {
            case CodeLineType.Function:
                PrintIndent(w, indent + 1);
                if (isData.FileVersion == 5)
                    w.Write($"{code.Name}(");
                else
                {
                    if (code.FunctionNumber != -1 &&
                        isData.FunctionPrototypes[code.FunctionNumber].Type == FunctionType.Dll &&
                        code.Name?.IndexOf('.') >= 0)
                        w.Write($"{code.Name.Substring(code.Name.IndexOf('.') + 1)}(");
                    else
                        w.Write($"{code.Name}(");
                    w.Write("begin\n");
                }

                PrintArgs(isData, code, w, function);
                w.WriteLine(");");
                break;

            case CodeLineType.Operation:
                PrintIndent(w, indent + 1);
                if (code.Destination != null)
                {
                    if (code.OperationNumber == OperationType.StrBw)
                    {
                        PrintArg(isData, code.Destination, w, function);
                        w.Write("[");
                        PrintArg(isData, code.Params[0], w, function);
                        w.Write("]");
                        w.Write("= ");
                        PrintArg(isData, code.Params[1], w, function);
                    }
                    else if (code is { OperationNumber: OperationType.StrBr, Params.Count: 2 })
                    {
                        PrintArg(isData, code.Destination, w, function);
                        w.Write("= ");
                        PrintArg(isData, code.Params[0], w, function);
                        w.Write("[");
                        PrintArg(isData, code.Params[1], w, function);
                        w.Write("]");
                    }
                    else if (code.OperationNumber == OperationType.StrLc ||
                             code.OperationNumber == OperationType.Special1 ||
                             code.OperationNumber == OperationType.SizeOf)
                    {
                        w.Write($"number0 = {code.StringOp}(");
                        PrintArg(isData, code.Destination, w, function);
                        for (var i = 0; i < code.Params.Count; i++)
                        {
                            w.Write(",");
                            PrintArg(isData, code.Params[i], w, function);
                        }

                        w.Write(" )");
                    }
                    else if (code.OperationNumber == OperationType.UseDll ||
                             code.OperationNumber == OperationType.Resize ||
                             code.OperationNumber == OperationType.ExeHandler ||
                             code.OperationNumber == OperationType.Handler ||
                             code.OperationNumber == OperationType.UnUseDll)
                    {
                        w.Write($"{code.StringOp} (");
                        PrintArg(isData, code.Destination, w, function);
                        for (var i = 0; i < code.Params.Count; i++)
                        {
                            w.Write(",");
                            PrintArg(isData, code.Params[i], w, function);
                        }

                        w.Write(" )");
                    }
                    else if (code.OperationNumber == OperationType.StrToNum ||
                             code.OperationNumber == OperationType.StrComp ||
                             code.OperationNumber == OperationType.StrSub ||
                             code.OperationNumber == OperationType.StrFind)
                    {
                        PrintArg(isData, code.Destination, w, function);
                        w.Write($" = {code.StringOp} (");
                        PrintArg(isData, code.Params[0], w, function);
                        for (var i = 1; i < code.Params.Count; i++)
                        {
                            w.Write(",");
                            PrintArg(isData, code.Params[i], w, function);
                        }

                        w.Write(" )");
                    }
                    else if (code.OperationNumber == OperationType.NumToStr)
                    {
                        w.Write("NumToStr(");
                        PrintArg(isData, code.Destination, w, function);
                        w.Write(",");
                        PrintArg(isData, code.Params[0], w, function);
                        w.Write(" )");
                    }
                    else if (code.OperationNumber == OperationType.StructMember1)
                    {
                        if (code.Params.Count == 2)
                        {
                            w.Write(" number0 = ");
                            PrintArg(isData, code.Params[1], w, function);
                            w.Write(".");
                            PrintArg(isData, code.Destination, w, function);
                            PrintArg(isData, code.Params[0], w, function);
                        }
                        else
                        {
                            w.Write($" number0 = {code.StringOp} (");
                            PrintArg(isData, code.Destination, w, function);
                            w.Write(".");
                            PrintArg(isData, code.Params[0], w, function);
                            for (var i = 1; i < code.Params.Count; i++)
                            {
                                w.Write(",");
                                PrintArg(isData, code.Params[i], w, function);
                            }

                            w.Write(" )");
                        }
                    }
                    else if (code.OperationNumber == OperationType.StructMember)
                    {
                        if (code.Params.Count >= 2)
                        {
                            PrintArg(isData, code.Params[1], w, function);
                            w.Write(".");
                            PrintArg(isData, code.Destination, w, function);
                            PrintArg(isData, code.Params[0], w, function);
                            w.Write("=");
                            for (var i = 2; i < code.Params.Count; i++)
                            {
                                if (i > 1) w.Write(",");
                                PrintArg(isData, code.Params[i], w, function);
                            }
                        }
                        else
                        {
                            w.Write("wrong code");
                        }
                    }
                    else if (code.OperationNumber == OperationType.If)
                    {
                        w.Write($"{code.StringOp} ");
                        if (code.Params.Count == 1)
                        {
                            PrintArg(isData, code.Params[0], w, function);
                            w.Write($" == false then goto label{code.Destination.IntVal + previous_label} ");
                        }
                    }
                    else
                    {
                        PrintArg(isData, code.Destination, w, function);
                        w.Write(" = ");
                        if (code.OperationNumber == OperationType.Equate)
                        {
                            if (code.Params.Count != 0)
                                PrintArg(isData, code.Params[0], w, function);
                        }
                        else
                        {
                            if (code.Params.Count == 2)
                            {
                                PrintArg(isData, code.Params[0], w, function);
                                w.Write($" {code.StringOp} ");
                                PrintArg(isData, code.Params[1], w, function);
                            }
                            else
                            {
                                w.Write($" {code.StringOp} ");
                                for (var i = 0; i < code.Params.Count; i++)
                                {
                                    if (i > 0) w.Write(" , ");
                                    PrintArg(isData, code.Params[i], w, function);
                                }
                            }
                        }
                    }
                }
                else
                {
                    w.Write($" {code.StringOp} ");
                }

                w.WriteLine(";");
                break;

            case CodeLineType.Exit:
                PrintIndent(w, indent + 1);
                w.WriteLine("exit;");
                break;
            case CodeLineType.Abort:
                PrintIndent(w, indent + 1);
                w.WriteLine("abort;");
                break;

            case CodeLineType.Label:
                if (isData.Labels[code.LabelNumber].Usage != 0)
                {
                    // Label reference chain printing is disabled until LRefPointer parsing is complete.
                    w.Write($"\nlabel{code.LabelNumber}: //Ref: ");
                    // var lRefP = isData.Labels[code.LabelNumber].LRefPointer;
                    // for (int i = 0; i < isData.Labels[code.LabelNumber].Usage; i++)
                    // {
                    //     w.Write($"{lRefP.Offset:X6}  ");
                    //     lRefP = lRefP.LRefPointer;
                    // }
                    w.WriteLine();
                }

                break;

            case CodeLineType.Goto:
                PrintIndent(w, indent + 1);
                w.WriteLine($"goto Label{code.DestLabel + previous_label};");
                break;

            case CodeLineType.IfStatement:
                PrintIndent(w, indent + 1);
                w.Write("if (");
                PrintArg(isData, code.Params[0], w, function);
                w.Write($" {Operations.Names[code.ComparisonType]} ");
                PrintArg(isData, code.Params[1], w, function);
                w.WriteLine(") then");
                PrintIndent(w, indent + 1);
                w.Write("             ");
                w.WriteLine($"goto Label{code.DestLabel};");
                PrintIndent(w, indent);
                w.Write("             ");
                w.WriteLine("endif;");
                break;

            case CodeLineType.Handler:
                PrintIndent(w, indent + 1);
                w.Write("Handler(");
                PrintArg(isData, code.Params[0], w, function);
                w.Write($", Label{code.Params[1].IntVal}");
                w.WriteLine(");");
                break;

            case CodeLineType.Return:
                PrintIndent(w, indent + 1);
                w.WriteLine("return;");
                break;

            case CodeLineType.FuncEnd:
                PrintIndent(w, indent + 1);
                w.WriteLine("end;");
                break;

            case CodeLineType.Call:
                PrintIndent(w, indent + 1);
                if (code.Opcode == 0x21) w.Write("call ");
                else w.Write("callDll ");

                var fn = code.Params[0].IntVal;
                if (fn < isData.FunctionPrototypes.Count)
                    w.Write(isData.FunctionPrototypes[fn].Name);
                else
                    w.Write($"Label{code.Params[0].IntVal}");

                w.Write("(");
                for (var i = 1; i < code.Params.Count; i++)
                {
                    PrintArg(isData, code.Params[i], w, function);
                    if (i != code.Params.Count - 1) w.Write(",");
                }

                w.Write(")");
                w.WriteLine(";");
                break;

            case CodeLineType.FuncReturn:
                PrintIndent(w, indent + 1);
                if (code.Opcode == 0x27)
                {
                    w.WriteLine("freeLocalVariable();");
                }
                else
                {
                    w.Write("return");
                    if (code.Params.Count == 1)
                    {
                        w.Write("(");
                        PrintArg(isData, code.Params[0], w, function);
                        w.Write(")");
                    }

                    w.WriteLine(";");
                }

                break;

            default:
                throw new InvalidOperationException($"Unknown code line type {code.Type}.");
        }
    }

    private static void PrintIndent(TextWriter w, int n)
    {
        for (var i = 0; i < n; i++) w.Write(" ");
    }

}
