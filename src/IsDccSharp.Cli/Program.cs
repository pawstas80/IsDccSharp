namespace IsDccSharp.Cli;

using IsDccSharp.Core;
using System;
using System.IO;
using System.Text;

internal static class Program
{
    private static int Main(string[] args)
    {
        WriteConsoleHeader(Console.Error);

        if (args.Length == 1 && IsHelpArgument(args[0]))
        {
            ShowUsage(Console.Error);
            return 0;
        }

        if (args.Length is < 1 or > 2)
        {
            ShowUsage(Console.Error);
            return 1;
        }

        var inputFile = args[0];
        var outputFile = args.Length == 2 ? args[1] : null;

        try
        {
            var decoder = new InxDecoder();
            var result = decoder.DecodeFile(inputFile);

            foreach (var warning in result.Warnings)
                Console.Error.WriteLine(warning);

            if (string.IsNullOrWhiteSpace(outputFile))
            {
                Console.Write(result.Text);
            }
            else
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputFile));
                if (!string.IsNullOrEmpty(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                File.WriteAllText(outputFile, ProjectInfo.AddFileHeader(result.Text), Encoding.ASCII);
                Console.WriteLine("Decoded file saved to: " + outputFile);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fatal: " + ex.Message);
            return 2;
        }
    }

    private static bool IsHelpArgument(string value) =>
        value.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("/?", StringComparison.OrdinalIgnoreCase);

    private static void ShowUsage(TextWriter writer)
    {
        writer.WriteLine("Usage:");
        writer.WriteLine("  IsDccSharp.exe <input.inx> [output.txt]");
        writer.WriteLine();
        writer.WriteLine("If output.txt is not provided, decoded text is written to the console.");
    }

    private static void WriteConsoleHeader(TextWriter writer)
    {
        foreach (var line in ProjectInfo.GetConsoleHeaderLines())
            writer.WriteLine(line);
    }
}
