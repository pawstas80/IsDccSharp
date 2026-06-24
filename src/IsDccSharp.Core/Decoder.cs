namespace IsDccSharp.Core;

using System;
using System.IO;
internal static class Decoder
{
    public static void AddCodeLine(ISData isData, int inCounter, CodeLine codeLine)
    {
        if (inCounter == -1)
        {
            if (isData.CodeLines.Count >= isData.CodeLinesMax)
            {
                isData.CodeLinesMax *= 2;
            }
            isData.CodeLines.Add(codeLine);
            return;
        }

        if (inCounter >= 0 && inCounter < isData.FunctionBodies.Count)
        {
            var funcBody = isData.FunctionBodies[inCounter];
            if (funcBody.CodeLines.Count >= funcBody.CodeLinesMax)
                funcBody.CodeLinesMax *= 2;

            funcBody.CodeLines.Add(codeLine);
        }
    }
    public static void Decode(BinaryReaderHelper reader, ISData isData)
    {
        DecodePass0(reader, isData);
        DecodePass1(isData);
        DecodePass2(isData);
    }
    private static void DecodePass0(BinaryReaderHelper reader, ISData isData)
    {
        var lastLabel = 0;

        while (true)
        {
            var pos = reader.Tell;
            var currFuncFileOffset = pos;

            if (pos >= isData.EOFPos)
                return;

            reader.Get2Byte();
            _ = reader.Get2Byte();
            reader.Seek(-2, SeekOrigin.Current);

            var fb = new FunctionBody
            {
                CodeLinesMax = 1
            };
            isData.FunctionBodies.Add(fb);
            var curFunction = isData.FunctionBodies.Count - 1;

            var expectFunctionStart = true;

            while (true)
            {
                pos = reader.Tell;

                if (pos >= isData.EOFPos)
                    return;

                // Label position synchronization
                for (var i = lastLabel; i < isData.Labels.Count; i++)
                {
                    if (isData.Labels[i].FilePosition == pos)
                    {
                        lastLabel = i + 1;
                        _ = reader.Get2Byte();
                    }
                    if (isData.Labels[i].FilePosition >= pos)
                    {
                        lastLabel = i;
                        break;
                    }
                }

                var opcode = reader.Get2Byte();

                // Detect new function start marker
                if (opcode == 0x0022 && !expectFunctionStart)
                {
                    reader.Seek(-2, SeekOrigin.Current);
                    break;
                }

                if (opcode == 0x0022 && expectFunctionStart)
                {
                    for (var j = 0; j < isData.FunctionPrototypes.Count; j++)
                    {
                        if (isData.FunctionPrototypes[j].Label == currFuncFileOffset)
                        {
                            isData.FunctionPrototypes[j].FunctionBody = isData.FunctionBodies[curFunction];
                            break;
                        }
                    }
                    expectFunctionStart = false;
                }

                if (!DecodeTable.Table.TryGetValue(opcode, out var entry))
                {
                    Util.Error($"\n!!!!Unknown opcode (0x{opcode:X}) at (0x{reader.Tell:X})\n");
                }
                else if (entry is { DecodeFunction: not null })
                    entry.DecodeFunction(reader, opcode, curFunction, isData);

                // 0x0026 indicates function end
                if (opcode == 0x0026)
                    break;
            }

            if (reader.Tell >= isData.EndCodeSegmentOffset && isData.EndCodeSegmentOffset != 0)
                break;
        }
    }
    public static void DecodePass1(ISData isData)
    {
        for (var i = 0; i < isData.CodeLines.Count; i++)
        {
            var cl = isData.CodeLines[i];
            if (cl is { Type: CodeLineType.Function, FunctionNumber: >= 0 } &&
                isData.FunctionPrototypes[cl.FunctionNumber].Type == FunctionType.User)
            {
                var fn = cl.FunctionNumber;

                if (isData.FunctionPrototypes[fn].FunctionBody != null)
                    continue;

                var labelIndex = cl.DestLabel;
                if (labelIndex < 0 || labelIndex >= isData.Labels.Count)
                    throw new InvalidOperationException($"Invalid function label index {labelIndex} at code line {i}.");

                if (isData.Labels[labelIndex].FunctionNumber == -1)
                    throw new InvalidOperationException("Invalid function label; no function is assigned.");

                var bodyIndex = isData.Labels[labelIndex].FunctionNumber;
                if (bodyIndex < 0 || bodyIndex >= isData.FunctionBodies.Count)
                    throw new InvalidOperationException($"Invalid function body index {bodyIndex} at code line {i}.");

                isData.FunctionPrototypes[fn].FunctionBody = isData.FunctionBodies[bodyIndex];

                isData.FunctionPrototypes[fn].LocalParamSize = isData.FunctionBodies[bodyIndex].LocalParamSize;
            }
        }

        // Process nested functions
        for (var i = 0; i < isData.FunctionBodies.Count; i++)
        {
            var body = isData.FunctionBodies[i];
            for (var j = 0; j < body.CodeLines.Count; j++)
            {
                var cl = body.CodeLines[j];
                if (cl is { Type: CodeLineType.Function, FunctionNumber: >= 0 } &&
                    isData.FunctionPrototypes[cl.FunctionNumber].Type == FunctionType.User)
                {
                    var fn = cl.FunctionNumber;
                    if (isData.FunctionPrototypes[fn].FunctionBody != null)
                        continue;

                    var labelIndex = cl.DestLabel;
                    if (labelIndex < 0 || labelIndex >= isData.Labels.Count)
                        throw new InvalidOperationException($"Invalid function label index {labelIndex} at function {i}.");

                    var bodyIndex = isData.Labels[labelIndex].FunctionNumber;
                    if (bodyIndex < 0 || bodyIndex >= isData.FunctionBodies.Count)
                        throw new InvalidOperationException($"Invalid function body index {bodyIndex} at function {i}.");

                    isData.FunctionPrototypes[fn].FunctionBody = isData.FunctionBodies[bodyIndex];
                    isData.FunctionPrototypes[fn].LocalParamSize = isData.FunctionBodies[bodyIndex].LocalParamSize;
                }
            }
        }
    }
    public static void DecodePass2(ISData isData)
    {
        for (var i = 0; i < isData.FunctionPrototypes.Count; i++)
        {
            var proto = isData.FunctionPrototypes[i];
            var body = proto.FunctionBody;

            if (body == null)
                continue;

            for (var j = 0; j < body.CodeLines.Count; j++)
            {
                var line = body.CodeLines[j];

                // Adjust parameters
                for (var k = 0; k < line.Params.Count; k++)
                {
                    var p = line.Params[k];
                    if (p == null) continue;

                    if (p.Type == ParamType.FnLocalStringVar)
                    {
                        if (p.VariableNumber < proto.ParamStringNames.Count)
                        {
                            p.Type = ParamType.FnParamStringVar;
                        }
                        else
                        {
                            p.VariableNumber -= proto.ParamStringNames.Count;
                        }
                    }

                    if (p.Type == ParamType.FnLocalNumberVar)
                    {
                        if (p.VariableNumber < proto.ParamNumberNames.Count)
                        {
                            p.Type = ParamType.FnParamNumberVar;
                        }
                        else
                        {
                            p.VariableNumber -= proto.ParamNumberNames.Count;
                        }
                    }
                }

                // Adjust destination
                var dest = line.Destination;
                if (dest != null)
                {
                    if (dest.Type == ParamType.FnLocalStringVar)
                    {
                        if (dest.VariableNumber < proto.ParamStringNames.Count)
                        {
                            dest.Type = ParamType.FnParamStringVar;
                        }
                        else
                        {
                            dest.VariableNumber -= proto.ParamStringNames.Count;
                        }
                    }

                    if (dest.Type == ParamType.FnLocalNumberVar)
                    {
                        if (dest.VariableNumber < proto.ParamNumberNames.Count)
                        {
                            dest.Type = ParamType.FnParamNumberVar;
                        }
                        else
                        {
                            dest.VariableNumber -= proto.ParamNumberNames.Count;
                        }
                    }
                }
            }
        }
    }
}
