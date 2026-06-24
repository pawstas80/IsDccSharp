namespace IsDccSharp.Core;

using System;
using System.Collections.Generic;
using System.IO;

internal static class Parser
{
    // The header stores a fixed 74-byte info field; shorter signatures are NUL-padded.
    private const int InfoStringFieldLength = 74;
    private const int OffsetTablePosition = 104;
    private const string InstallShieldSignature = "Copyright (c) 1990-2002 InstallShield Software Corp. All Rights Reserved.";
    private const string StirlingSignature = "Copyright (c) 1990-1999 Stirling Technologies, Ltd. All Rights Reserved.";

    public static void ParseHeader(BinaryReaderHelper reader, ISData isData)
    {
        isData.CodeLinesMax = 1;
        isData.FileVersion = 12;

        isData.CRC = reader.Get4Byte();
        _ = reader.Get2Byte();

        var info = reader.GetString(InfoStringFieldLength);
        isData.InfoString = Util.FilterLF(info).TrimEnd('\0');
        if (!isData.InfoString.StartsWith(InstallShieldSignature, StringComparison.Ordinal) &&
            !isData.InfoString.StartsWith(StirlingSignature, StringComparison.Ordinal))
        {
            isData.Warnings.Add("Warning: unrecognized INX info string; decoding will continue.");
        }

        reader.Seek(OffsetTablePosition);

        var offsets = new uint[5];
        for (var i = 0; i < offsets.Length; i++)
            offsets[i] = reader.Get4Byte();
        _ = reader.Get2Byte();

        reader.Seek(offsets[2]);

        isData.EndCodeSegmentOffset = (int)offsets[4];
        if (isData.EndCodeSegmentOffset == 0)
        {
            var cur = reader.Tell;
            reader.Seek(0, SeekOrigin.End);
            isData.EndCodeSegmentOffset = (int)reader.Tell;
            reader.Seek(cur);
        }

        var dataTypesCount = reader.Get2Byte();
        isData.DataTypes = ParseUserTypes(reader, dataTypesCount);

        var funcCount = reader.Get2Byte();
        isData.FunctionPrototypes = ParseFunctions(reader, funcCount);
        isData.FunctionBodies = new List<FunctionBody>(funcCount);

        var labelCount = reader.Get2Byte();
        isData.Labels = new List<Label>(labelCount);
        for (var i = 0; i < labelCount; i++)
        {
            isData.Labels.Add(new Label
            {
                Usage = 0,
                Position = -1,
                Passed = 0,
                FilePosition = (int)reader.Get4Byte()
            });
        }

        foreach (var f in isData.FunctionPrototypes)
        {
            if (f.Label != 0xFFFF && f.Label < isData.Labels.Count)
                f.Label = isData.Labels[f.Label].FilePosition;
        }

        var curPos = reader.Tell;
        reader.Seek(0, SeekOrigin.End);
        isData.EOFPos = reader.Tell;
        reader.Seek(curPos);
    }

    public static List<FunctionPrototype> ParseFunctions(BinaryReaderHelper reader, int funcCount)
    {
        var functions = new List<FunctionPrototype>(funcCount);

        for (var i = 0; i < funcCount; i++)
        {
            var f = new FunctionPrototype
            {
                Offset = reader.Tell,
                FunctionBody = null,
                Type = (FunctionType)reader.Get1Byte(),
                ReturnType = reader.Get1Byte()
            };

            var dllNameLen = reader.Get2Byte();
            var dllName = dllNameLen != 0 ? reader.GetString(dllNameLen) + "." : string.Empty;

            var funcNameLen = reader.Get2Byte();
            f.Name = funcNameLen != 0
                ? dllName + reader.GetString(funcNameLen)
                : $"function{i}";

            f.Label = reader.Get2Byte();
            f.ParamsCount = reader.Get2Byte();

            f.Params = [];
            f.ParamStringNamesCount = 0;
            f.ParamNumberNamesCount = 0;

            for (var j = 0; j < f.ParamsCount; j++)
            {
                var paramType = (int)ParseType(reader.Get1Byte());

                if ((reader.Get1Byte() & 2) != 0)
                    paramType |= unchecked((int)0x80000000);

                f.Params.Add(paramType);

                if ((paramType & 0xFFFFFF) == (int)VarType.fixstr)
                    f.ParamStringNamesCount++;
                else
                    f.ParamNumberNamesCount++;
            }

            f.ParamStringNames = new List<string>(f.ParamStringNamesCount);
            f.ParamNumberNames = new List<string>(f.ParamNumberNamesCount);

            var paramCounts = new int[10];

            for (var j = 0; j < f.ParamsCount; j++)
            {
                var baseType = f.Params[j] & 0xFFFFFF;
                var parameterName = baseType switch
                {
                    (int)VarType.fixstr => $"pString{paramCounts[0]++}",
                    (int)VarType.Char => $"pChar{paramCounts[1]++}",
                    (int)VarType.Long => $"pLong{paramCounts[2]++}",
                    (int)VarType.Int => $"pInt{paramCounts[3]++}",
                    (int)VarType.number => $"pNumber{paramCounts[4]++}",
                    (int)VarType.LIST => $"pList{paramCounts[5]++}",
                    (int)VarType.BOOL => $"pBool{paramCounts[6]++}",
                    (int)VarType.HWND => $"pHwnd{paramCounts[7]++}",
                    (int)VarType.CONSTANT => $"pConstant{paramCounts[7]++}",
                    (int)VarType.UNDEF1 => $"pUndef1{paramCounts[7]++}",
                    (int)VarType.UNDEF2 => $"pUndef2{paramCounts[7]++}",
                    (int)VarType.UNDEF3 => $"pUndef3{paramCounts[7]++}",
                    (int)VarType.UNDEF4 => $"pUndef4{paramCounts[7]++}",
                    (int)VarType.UNDEF5 => $"pUndef5{paramCounts[7]++}",
                    _ => throw new InvalidOperationException($"Unknown parameter type {baseType}")
                };

                if (baseType == (int)VarType.fixstr)
                    f.ParamStringNames.Add(parameterName);
                else
                    f.ParamNumberNames.Add(parameterName);
            }

            functions.Add(f);
        }

        return functions;
    }

    private static List<DataType> ParseUserTypes(BinaryReaderHelper reader, int numTypes)
    {
        var list = new List<DataType>(numTypes);
        for (var i = 0; i < numTypes; i++)
        {
            var dt = new DataType();
            var entries = reader.Get2Byte();
            for (var j = 0; j < entries; j++)
            {
                var e = new TypeEntry
                {
                    Offset = reader.Tell,
                    Type = ParseType(reader.Get1Byte()),
                    Size = reader.Get2Byte()
                };
                int nameLen = reader.Get2Byte();
                e.Name = reader.GetString(nameLen);
                dt.Entries.Add(e);
            }

            list.Add(dt);
        }

        return list;
    }

    private static VarType ParseType(int type) => type switch
    {
        0 => VarType.fixstr,
        1 => VarType.Char,
        2 => VarType.Long,
        3 => VarType.Int,
        4 => VarType.number,
        5 => VarType.LIST,
        6 => VarType.BOOL,
        7 => VarType.HWND,
        8 => VarType.UNDEF1,
        9 => VarType.CONSTANT,
        10 => VarType.UNDEF2,
        11 => VarType.UNDEF3,
        12 => VarType.UNDEF4,
        13 => VarType.UNDEF5,
        _ => throw new InvalidOperationException($"Unknown VarType {type}")
    };
}
