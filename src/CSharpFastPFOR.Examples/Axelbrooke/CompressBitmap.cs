using System.Diagnostics;
using System.IO.Compression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;

namespace Genbox.CSharpFastPFOR.Examples.Axelbrooke;

internal static class CompressBitmap
{
    private static readonly IntegratedIntCompressor iic = new IntegratedIntCompressor(
        new SkippableIntegratedComposition(
            new IntegratedBinaryPacking(),
            new IntegratedVariableByte()));

    public static void main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("usage: please provide the file name of a bitmap binary file.");
            return;
        }
        Console.WriteLine("loading file " + args[0] + " as a bitmap");
        int[] data = fromBitsetFileToArray(args[0]);
        Console.WriteLine("Compressing " + data.Length + " integers");
        int[] compressed = iic.compress(data);
        int[] recov = iic.uncompress(compressed);
        Console.WriteLine("compressed from " + ((data.Length * 4) / 1024) + "KB to " + ((compressed.Length * 4) / 1024) + "KB");
        Console.WriteLine("ratio: " + Math.Round((data.Length * 1.0) / compressed.Length));

        if (!Arrays.equals(recov, data)) throw new InvalidOperationException("bug");

        long bef = Stopwatch.GetTimestamp();
        iic.uncompress(compressed);
        long aft = Stopwatch.GetTimestamp();

        Console.WriteLine("decoding speed:" + Math.Round((data.Length * 1000.0 * Stopwatch.Frequency) / ((aft - bef) * 1_000_000_000.0)) + " millions of integers per second");

        bef = Stopwatch.GetTimestamp();
        iic.compress(data);
        aft = Stopwatch.GetTimestamp();

        Console.WriteLine("encoding speed:" + Math.Round((data.Length * 1000.0 * Stopwatch.Frequency) / ((aft - bef) * 1_000_000_000.0)) + " millions of integers per second");

        Console.WriteLine("note: with a bit of effort, speed can be much higher.");

        Console.WriteLine();
        zipStats(args[0]);
    }

    private static int[] fromBitsetFileToArray(string filename)
    {
        byte[] data = File.ReadAllBytes(filename);
        // we determine cardinality
        int card = 0;
        foreach (byte value in data)
        {
            int bv = value;
            card += int.PopCount(bv);
        }
        int[] answer = new int[card];
        int pos = 0;
        for (int k = 0; k < data.Length; ++k)
        {
            int bv = data[k] & 0xFF;
            for (int b = 0; b < 8; ++b)
            {
                if (((bv >> b) & 1) == 1)
                    answer[pos++] = b + (k * 8);
            }
        }
        if (pos != card) throw new InvalidOperationException("bug");
        return answer;
    }

    private static void zipStats(string filename)
    {
        byte[] input = File.ReadAllBytes(filename);
        Console.WriteLine("I will try to compress the original bitmap using zip.");

        long bef = Stopwatch.GetTimestamp();
        using MemoryStream baos = new MemoryStream();
        using (ZipArchive zos = new ZipArchive(baos, ZipArchiveMode.Create, true))
        {
            ZipArchiveEntry entry = zos.CreateEntry(filename, CompressionLevel.SmallestSize);
            using Stream entryStream = entry.Open();
            entryStream.Write(input);
        }
        byte[] result = baos.ToArray();
        long aft = Stopwatch.GetTimestamp();
        Console.WriteLine("zip encoding speed:" + ((input.Length * 1000.0 * Stopwatch.Frequency) / ((aft - bef) * 1_000_000_000.0)) + " million of bytes per second");
        Console.WriteLine("zip compression ratio at best level : " + ((input.Length * 1.0) / result.Length));
    }
}