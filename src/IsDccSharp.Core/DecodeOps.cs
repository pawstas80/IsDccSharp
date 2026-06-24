namespace IsDccSharp.Core;

using System;
using System.IO;

internal static class DecodeOps
{
    private static int ParseOther(BinaryReaderHelper r, byte[] tmpBuf)
    {
        int tag = r.Get1Byte();
        int value;

        switch (tag)
        {
            case 0x00:
                value = r.Get1Byte() << 8;
                break;
            case 0x80:
            case 0x70:
                value = r.Get2Byte();
                break;
            case 0x07:
                value = (int)r.Get4Byte();
                break;
            default:
                return (int)OtherType.Unknown;
        }

        tmpBuf[0] = (byte)(value & 0xFF);
        tmpBuf[1] = (byte)((value >> 8) & 0xFF);
        tmpBuf[2] = (byte)((value >> 16) & 0xFF);
        tmpBuf[3] = (byte)((value >> 24) & 0xFF);

        return tag switch
        {
            0x00 => (int)OtherType.NumParams,
            0x80 => (int)OtherType.UserFunction,
            0x70 => (int)OtherType.Label,
            0x07 => (int)OtherType.Label,
            _ => (int)OtherType.Unknown
        };
    }

    private static int ParseArg(BinaryReaderHelper r, Parameter p, ISData isData, bool updateUsage)
    {
        int tag = r.Get1Byte();

        switch (tag)
        {
            case 0x00:
                p.Type = ParamType.LongConst;
                p.IntVal = r.Get1Byte();
                return (int)p.Type;

            case 0x06:
                {
                    p.Type = ParamType.StringConst;
                    var len = r.Get2Byte();
                    p.StringVal = r.GetString(len);
                    return (int)p.Type;
                }

            case 0x07:
                p.Type = ParamType.LongConst;
                p.IntVal = (int)r.Get4Byte();
                return (int)p.Type;

            case 0x02:
                {
                    var idx = r.Get2Byte();
                    p.Type = idx < isData.StringSysVars.Count ? ParamType.SystemStringVar : ParamType.UserStringVar;

                    p.VariableNumber = idx;

                    if (updateUsage && p.Type == ParamType.UserStringVar && idx < isData.StringUserVars.Count)
                        isData.StringUserVars[idx]++;

                    return (int)p.Type;
                }

            case 0x03:
                {
                    var idx = r.Get2Byte();
                    p.Type = idx < isData.NumberSysVars.Count ? ParamType.SystemNumberVar : ParamType.UserNumberVar;

                    p.VariableNumber = idx;

                    if (updateUsage && p.Type == ParamType.UserNumberVar && idx < isData.NumberUserVars.Count)
                        isData.NumberUserVars[idx]++;

                    return (int)p.Type;
                }

            case 0x04:
                {
                    var idx = r.Get2Byte();
                    p.Type = ParamType.FnLocalStringVar;
                    p.VariableNumber = idx;
                    if ((0xFF9B - idx) < 0x8000) p.VariableNumber = 0xFF9B - idx;
                    return (int)p.Type;
                }

            case 0x05:
                {
                    var idx = r.Get2Byte();
                    p.Type = ParamType.FnLocalNumberVar;
                    p.VariableNumber = idx;
                    if ((0xFF9B - idx) < 0x8000) p.VariableNumber = 0xFF9B - idx;
                    return (int)p.Type;
                }

            case 0x08:
                {
                    var tmp = r.Get2Byte();

                    if ((0xFF9B - tmp) < 0x8000)
                    {
                        p.Type = ParamType.FnLocalNumberVar;
                        p.VariableNumber = 0xFF9B - tmp;
                        return (int)p.Type;
                    }

                    if (tmp < isData.NumberSysVars.Count)
                    {
                        p.Type = ParamType.SystemNumberVar;
                        p.VariableNumber = tmp;
                        return (int)p.Type;
                    }

                    p.Type = ParamType.UserNumberVar;
                    p.VariableNumber = tmp;
                    return (int)p.Type;
                }

            case 0x0A:
                {
                    p.Type = ParamType.Label;
                    p.IntVal = r.Get2Byte();
                    return (int)p.Type;
                }

            default:
                p.Type = ParamType.Unknown;
                return (int)p.Type;
        }
    }

    public static void DecodeOp(BinaryReaderHelper r, int opcode, int curFunction, ISData isData)
    {
        var cl = new CodeLine
        {
            Type = CodeLineType.Operation,
            Offset = r.Tell - 2,
            Opcode = opcode,
            Params = [],
            ParamsCount = r.Get2Byte() - 1
        };

        if (DecodeTable.Table.TryGetValue(opcode, out var entry))
        {
            cl.OperationNumber = entry.OperationNumber;
            cl.StringOp = Operations.Names[(int)cl.OperationNumber] ?? "";
        }
        else
        {
            cl.OperationNumber = OperationType.Unknown;
            cl.StringOp = "";
        }

        if (cl.OperationNumber == OperationType.Pointer)
        {
            r.Seek(-2, SeekOrigin.Current);
            cl.ParamsCount = 1;

            cl.Destination = new Parameter();
            cl.Params.Add(new Parameter
            {
                IntVal = r.Get2Byte(),
                Type = ParamType.DataTypeNum,
                ByRef = false
            });

            ParseArg(r, cl.Destination, isData, true);
        }
        else if (cl.OperationNumber == OperationType.Unknown || opcode == 0x62)
        {
            r.Seek(-2, SeekOrigin.Current);
            cl.ParamsCount = 0;
            cl.Destination = null;
        }
        else if ((opcode == 0x0004 && cl.ParamsCount > 1) ||
                 ((opcode is >= 0x0007 and < 0x0020 || opcode == 0x2A || opcode == 0x2B) &&
                  cl.ParamsCount > 2))
        {
            r.Seek(-2, SeekOrigin.Current);
            cl.ParamsCount = 0;
            cl.Destination = null;
        }
        else
        {
            if (opcode == 0x0004 && cl.ParamsCount == 1)
            {
                var val = r.Get2Byte();
                if (val == 0)
                {
                    cl.ParamsCount = 0;
                    cl.Destination = new Parameter
                    {
                        IntVal = val,
                        Type = ParamType.DataTypeNum,
                        ByRef = false
                    };
                    goto OutputLine;
                }
                else
                {
                    r.Seek(-2, SeekOrigin.Current);
                }
            }

            if (cl.ParamsCount >= 0)
            {
                cl.Destination = new Parameter();
                ParseArg(r, cl.Destination, isData, true);

                if (cl.ParamsCount > 0)
                {
                    for (var i = 0; i < cl.ParamsCount; i++)
                    {
                        var p = new Parameter
                        {
                            ReadOffset = r.Tell
                        };
                        ParseArg(r, p, isData, true);
                        cl.Params.Add(p);
                    }
                }
            }
            else
            {
                cl.ParamsCount = 0;
                cl.Destination = null;
            }
        }

    OutputLine:
        Decoder.AddCodeLine(isData, curFunction, cl);
    }

    public static void Equate(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        var cl = new CodeLine { Type = CodeLineType.Operation, Offset = r.Tell - 2, Opcode = opcode, OperationNumber = OperationType.Equate, StringOp = "=" };
        var nb = r.Get2Byte();
        if (nb == 1)
        {
            cl.Destination = new Parameter { Type = ParamType.SystemNumberVar, VariableNumber = r.Get2Byte() };
        }
        else
        {
            cl.Destination = new Parameter();
            ParseArg(r, cl.Destination, isData, true);

            var p = new Parameter();
            ParseArg(r, p, isData, true);
            cl.Params.Add(p);
        }
        Decoder.AddCodeLine(isData, inCounter, cl);
    }

    public static void DoGoto(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        var cl = new CodeLine { Type = CodeLineType.Goto, Offset = r.Tell - 2, Opcode = opcode };
        var a = r.Get1Byte();
        if (a == 0)
        {
            r.Get1Byte();
            cl.DestLabel = 0;
        }
        else
        {
            r.Seek(-1, SeekOrigin.Current);

            if (a == 0x07 || a == 0x70)
            {
                var b = new byte[4];
                if (ParseOther(r, b) != (int)OtherType.Label)
                    Util.Error("Goto expected label");

                cl.DestLabel = BitConverter.ToInt32(b, 0);
            }
            else if (opcode == 1)
            {
                cl.DestLabel = 0;
            }
            else
            {
                var b = new byte[4];
                if (ParseOther(r, b) != (int)OtherType.Label)
                    Util.Error("Goto expected label");

                cl.DestLabel = BitConverter.ToInt32(b, 0);
            }
        }

        Decoder.AddCodeLine(isData, inCounter, cl);
    }

    public static void DoGoto2(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        var cl = new CodeLine { Type = CodeLineType.Goto, Offset = r.Tell - 2, Opcode = opcode };
        var a = r.Get2Byte();
        if (a == 1)
        {
            var b = new byte[4];
            var ot = ParseOther(r, b);
            cl.DestLabel = BitConverter.ToInt32(b, 0);
            // Some newer INX files place the next opcode immediately after this label.
            // Do not probe past the parsed label, or the decoder loses byte alignment.
            if (ot != (int)OtherType.Label && ot != (int)OtherType.NumParams)
                Util.Error("Goto expected label");
        }
        else
        {
            r.Seek(-2, SeekOrigin.Current);
            cl.DestLabel = 0;
        }

        Decoder.AddCodeLine(isData, inCounter, cl);
    }

    public static void FunctionStart(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        r.Get2Byte();
        var mark = r.Get1Byte();
        if (mark != 7)
        {
            Util.Error($"Unexpected function start marker 0x{mark:X}");
        }
        var localParamSize = (int)r.Get4Byte();
        var idx = inCounter < 0 ? 0 : inCounter;
        while (isData.FunctionBodies.Count <= idx) isData.FunctionBodies.Add(new FunctionBody());
        isData.FunctionBodies[idx].LocalParamSize = [localParamSize];
    }

    public static void FunctionEnd(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        _ = r.Get2Byte();
        _ = r.Get2Byte();
        var nb2 = r.Get2Byte();
        r.Skip(nb2 * 4);

        _ = r.Get2Byte();
        var nb4 = r.Get2Byte();
        r.Skip(nb4 * 4);

        var cl = new CodeLine { Type = CodeLineType.FuncEnd, Offset = r.Tell - 2, Opcode = opcode };
        Decoder.AddCodeLine(isData, inCounter, cl);
    }

    public static void FuncReturn(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        var cl = new CodeLine { Type = CodeLineType.FuncReturn, Offset = r.Tell - 2, Opcode = opcode };
        if (opcode == 0x0027)
        {
            r.Get2Byte();
        }
        else if (opcode != 0x0025)
        {
            var cnt = r.Get2Byte();
            if (cnt > 0xa) r.Seek(-2, System.IO.SeekOrigin.Current);
            else if (cnt != 0)
            {
                for (var i = 0; i < cnt; i++)
                {
                    var p = new Parameter();
                    ParseArg(r, p, isData, true);
                    cl.Params.Add(p);
                }
            }

        }
        Decoder.AddCodeLine(isData, inCounter, cl);
    }

    public static void DoExit(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        var cl = new CodeLine { Type = CodeLineType.Exit, Offset = r.Tell - 2, Opcode = opcode };
        var param = r.Get2Byte();
        if (param > 1)
        {
            r.Seek(-2, SeekOrigin.Current);
        }
        else
        {
            for (var i = 0; i < param; i++)
                r.Get2Byte();
        }

        Decoder.AddCodeLine(isData, inCounter, cl);
    }

    public static void DoAbort(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        var cl = new CodeLine { Type = CodeLineType.Abort, Offset = r.Tell - 2, Opcode = opcode };
        var param = r.Get2Byte();
        if (param != 0)
            r.Seek(-2, SeekOrigin.Current);

        Decoder.AddCodeLine(isData, inCounter, cl);
    }

    public static void DoCall(BinaryReaderHelper r, int opcode, int inCounter, ISData isData)
    {
        var cl = new CodeLine { Type = CodeLineType.Call, Offset = r.Tell - 2, Opcode = opcode };
        var dest = r.Get2Byte();
        var nb = r.Get2Byte();
        cl.Params.Add(new Parameter { Type = ParamType.LongConst, IntVal = dest });
        for (var i = 0; i < nb; i++)
        {
            var p = new Parameter();
            ParseArg(r, p, isData, true);
            cl.Params.Add(p);
        }

        if (dest < isData.Labels.Count) isData.Labels[dest].Usage++;
        Decoder.AddCodeLine(isData, inCounter, cl);
    }
}
