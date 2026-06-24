namespace IsDccSharp.Core;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class InxDecoder
{
    private const string DecodedInxHeader = "aLuZ";

    public DecodeResult DecodeFile(string inputFile, DecodeOptions? options = null)
    {
        if (inputFile == null)
            throw new ArgumentNullException(nameof(inputFile));

        if (!File.Exists(inputFile))
            throw new FileNotFoundException("File not found.", inputFile);

        var buffer = File.ReadAllBytes(inputFile);
        return DecodeBuffer(buffer, inputFile, buffer.LongLength, options);
    }

    public async Task<DecodeResult> DecodeFileAsync(
        string inputFile,
        DecodeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (inputFile == null)
            throw new ArgumentNullException(nameof(inputFile));

        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(inputFile))
            throw new FileNotFoundException("File not found.", inputFile);

        var buffer = await ReadAllBytesAsync(inputFile, cancellationToken).ConfigureAwait(false);
        return await Task.Run(
            () => DecodeBuffer(buffer, inputFile, buffer.LongLength, options, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public DecodeResult DecodeBytes(byte[] buffer, string? sourceName = null, DecodeOptions? options = null)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        return DecodeBuffer(buffer, sourceName, buffer.LongLength, options);
    }

    private static DecodeResult DecodeBuffer(
        byte[] input,
        string? sourceName,
        long inputLength,
        DecodeOptions? options,
        CancellationToken cancellationToken = default)
    {
        var decodeOptions = options ?? new DecodeOptions();
        decodeOptions.Validate();

        cancellationToken.ThrowIfCancellationRequested();

        var buffer = input;
        var hasDecodedHeader = HasDecodedInxHeader(buffer);
        var wasUnscrambled = false;

        if (decodeOptions.AutoUnscramble && ShouldTryUnscramble(sourceName) && !hasDecodedHeader)
        {
            buffer = Util.UnScramble(buffer);
            wasUnscrambled = true;
            hasDecodedHeader = HasDecodedInxHeader(buffer);
        }

        using var stream = new MemoryStream(buffer);
        using var reader = new BinaryReaderHelper(stream);
        var isData = new ISData();

        Parser.ParseHeader(reader, isData);
        cancellationToken.ThrowIfCancellationRequested();

        Decoder.Decode(reader, isData);
        cancellationToken.ThrowIfCancellationRequested();

        using var outputStream = new MemoryStream();
        using var writer = new StreamWriter(outputStream, Encoding.ASCII);
        Output.Generate(
            isData,
            writer,
            decodeOptions.IndentWidth,
            decodeOptions.IncludeStringUserVariables,
            decodeOptions.IncludeNumberUserVariables,
            decodeOptions.IncludeDataTypes,
            decodeOptions.IncludeFunctionPrototypes,
            decodeOptions.IncludeFunctionDefinitions);
        writer.Flush();

        var decodedText = Encoding.ASCII.GetString(outputStream.ToArray());
        return new DecodeResult(
            decodedText,
            sourceName,
            inputLength,
            wasUnscrambled,
            hasDecodedHeader,
            isData.InfoString,
            isData.Warnings);
    }

    private static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        if (stream.Length > int.MaxValue)
            throw new IOException("Input file is too large to load into memory.");

        var buffer = new byte[(int)stream.Length];
        var offset = 0;

        while (offset < buffer.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException("Unexpected end of file while reading input.");

            offset += read;
        }

        return buffer;
    }

    private static bool ShouldTryUnscramble(string? sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return false;

        return Path.GetExtension(sourceName).Equals(".inx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasDecodedInxHeader(byte[] buffer)
    {
        if (buffer.Length < DecodedInxHeader.Length)
            return false;

        for (var i = 0; i < DecodedInxHeader.Length; i++)
        {
            if (buffer[i] != (byte)DecodedInxHeader[i])
                return false;
        }

        return true;
    }
}
