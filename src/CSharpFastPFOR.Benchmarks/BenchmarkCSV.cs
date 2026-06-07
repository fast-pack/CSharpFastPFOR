/*
 * This will run benchmarks using a set of posting lists stored as CSV files.
 *
 * @author lemire
 *
 */

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using System.Globalization;

namespace Genbox.CSharpFastPFOR.Benchmarks;

internal enum Format
{
    ONEARRAYPERLINE,
    ONEARRAYPERFILE,
    ONEINTPERLINE
}

internal enum CompressionMode
{
    AS_IS,
    DELTA
}

internal static class BenchmarkCSV
{
    private static readonly IntegratedIntegerCODEC[] codecs = [new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte())];
    private static readonly IntegratedByteIntegerCODEC[] bcodecs = [new IntegratedVariableByte()];
    private static readonly IntegerCODEC[] regcodecs =
    [
        new Composition(new FastPFOR128(), new VariableByte()),
        new Composition(new FastPFOR(), new VariableByte()),
        new Composition(new BinaryPacking(), new VariableByte())
    ];
    private static readonly ByteIntegerCODEC[] regbcodecs = [new VariableByte()];

    /*
     * @param args command-line arguments
     */
    public static void main(string[] args)
    {
        Format myformat = Format.ONEARRAYPERLINE;
        CompressionMode cm = CompressionMode.DELTA;
        List<string> files = [];
        foreach (string s in args)
        {
            if (s.StartsWith('-'))
            {
                // it is a flag
                if (s.Equals("--onearrayperfile", StringComparison.Ordinal))
                    myformat = Format.ONEARRAYPERFILE;
                else if (s.Equals("--nodelta", StringComparison.Ordinal))
                    cm = CompressionMode.AS_IS;
                else if (s.Equals("--oneintperline", StringComparison.Ordinal))
                    myformat = Format.ONEINTPERLINE;
                else
                    throw new ArgumentException("I don't understand: " + s, nameof(args));
            }
            else
            {
                // it is a filename
                files.Add(s);
            }
        }
        if (myformat == Format.ONEARRAYPERFILE)
            Console.WriteLine("Treating each file as one array.");
        else if (myformat == Format.ONEARRAYPERLINE)
            Console.WriteLine("Each line of each file is an array: use --onearrayperfile or --oneintperline to change.");
        else if (myformat == Format.ONEINTPERLINE)
            Console.WriteLine("Treating each file as one array, with one integer per line.");
        Console.WriteLine(cm == CompressionMode.AS_IS
            ? "Compressing the integers 'as is' (no differential coding)"
            : "Using differential coding (arrays will be sorted): use --nodelta to prevent sorting");
        List<int[]> data = [];
        foreach (string fn in files)
        foreach (int[] x in loadIntegers(fn, myformat))
            data.Add(x);
        Console.WriteLine("Loaded " + data.Count + " array(s)");
        if (cm == CompressionMode.DELTA)
        {
            Console.WriteLine("Sorting the arrray(s) because you are using differential coding");
            foreach (int[] x in data)
                Arrays.sort(x);
        }
        bench(data, cm, false);
        bench(data, cm, false);
        bench(data, cm, true);
        bytebench(data, cm, false);
        bytebench(data, cm, false);
        bytebench(data, cm, true);
    }

    private static List<int[]> loadIntegers(string filename, Format f)
    {
        int misparsed = 0;
        if (f == Format.ONEARRAYPERLINE)
        {
            List<int[]> answer = [];

            foreach (string s in File.ReadLines(filename))
            {
                string[] numbers = s.Split(',', ';'); // that's slow
                int[] a = new int[numbers.Length];
                for (int k = 0; k < numbers.Length; ++k)
                {
                    try
                    {
                        a[k] = int.Parse(numbers[k].Trim(), CultureInfo.InvariantCulture);
                    }
                    catch (Exception nfe)
                    {
                        if (misparsed == 0)
                            Console.Error.WriteLine(nfe);

                        ++misparsed;
                    }
                }
                answer.Add(a);
            }
            if (misparsed > 0)
                Console.WriteLine("Failed to parse " + misparsed + " entries");

            return answer;
        }

        if (f == Format.ONEARRAYPERFILE)
        {
            List<int> answer = [];
            foreach (string s in File.ReadLines(filename))
            {
                string[] numbers = s.Split(',', ';'); // that's slow
                foreach (string number in numbers)
                {
                    try
                    {
                        answer.Add(int.Parse(number.Trim(), CultureInfo.InvariantCulture));
                    }
                    catch (Exception nfe)
                    {
                        if (misparsed == 0)
                            Console.Error.WriteLine(nfe);
                        ++misparsed;
                    }
                }
            }
            int[] actualanswer = new int[answer.Count];
            answer.CopyTo(actualanswer);
            List<int[]> wrap =
            [
                actualanswer
            ];
            if (misparsed > 0)
                Console.WriteLine("Failed to parse " + misparsed + " entries");
            return wrap;
        }
        else
        {
            List<int> answer = [];
            foreach (string s in File.ReadLines(filename))
            {
                try
                {
                    answer.Add(int.Parse(s.Trim(), CultureInfo.InvariantCulture));
                }
                catch (Exception nfe)
                {
                    if (misparsed == 0)
                        Console.Error.WriteLine(nfe);
                    ++misparsed;
                }
            }
            int[] actualanswer = new int[answer.Count];
            answer.CopyTo(actualanswer);
            List<int[]> wrap =
            [
                actualanswer
            ];

            if (misparsed > 0)
                Console.WriteLine("Failed to parse " + misparsed + " entries");
            return wrap;
        }
    }

    private static void bench(List<int[]> postings, CompressionMode cm, bool verbose)
    {
        int maxlength = 0;
        foreach (int[] x in postings)
        {
            if (maxlength < x.Length)
                maxlength = x.Length;
        }
        if (verbose)
            Console.WriteLine("Max array length: " + maxlength);
        int[] compbuffer = new int[(2 * maxlength) + 1024];
        int[] decompbuffer = new int[maxlength];
        if (verbose)
            Console.WriteLine("Scheme -- bits/int -- speed (mis)");

        foreach (IntegerCODEC c in cm == CompressionMode.DELTA ? codecs : regcodecs)
        {
            long decomptime = 0;
            long volumein = 0;
            long volumeout = 0;
            int[][] compdata = new int[postings.Count][];
            for (int k = 0; k < postings.Count; ++k)
            {
                int[] @in = postings[k];
                IntWrapper inpos = new IntWrapper(0);
                IntWrapper outpos = new IntWrapper(0);
                c.compress(@in, inpos, @in.Length, compbuffer,
                    outpos);
                int clength = outpos.get();
                inpos = new IntWrapper(0);
                outpos = new IntWrapper(0);
                c.uncompress(compbuffer, inpos, clength,
                    decompbuffer, outpos);
                volumein += @in.Length;
                volumeout += clength;

                if (outpos.get() != @in.Length)
                    throw new InvalidOperationException("bug");
                for (int z = 0; z < @in.Length; ++z)
                {
                    if (@in[z] != decompbuffer[z])
                    {
                        throw new InvalidOperationException(
                            "bug");
                    }
                }
                compdata[k] = Arrays
                    .copyOf(compbuffer, clength);
            }

            long bef = Port.System.nanoTime();
            foreach (int[] cin in compdata)
            {
                IntWrapper inpos = new IntWrapper(0);
                IntWrapper outpos = new IntWrapper(0);
                c.uncompress(cin, inpos, cin.Length,
                    decompbuffer, outpos);
                if (inpos.get() != cin.Length)
                    throw new InvalidOperationException("bug");
            }
            long aft = Port.System.nanoTime();
            decomptime += aft - bef;
            double bitsPerInt = (volumeout * 32.0) / volumein;
            double decompressSpeed = (volumein * 1000.0) / decomptime;
            if (verbose)
                Console.WriteLine($"{c}\t\t{bitsPerInt:0.00}\t{decompressSpeed:0.00}");
        }
    }

    private static void bytebench(List<int[]> postings, CompressionMode cm, bool verbose)
    {
        int maxlength = 0;
        foreach (int[] x in postings)
        {
            if (maxlength < x.Length)
                maxlength = x.Length;
        }
        if (verbose)
            Console.WriteLine("Max array length: " + maxlength);
        sbyte[] compbuffer = new sbyte[6 * (maxlength + 1024)];
        int[] decompbuffer = new int[maxlength];
        if (verbose)
            Console.WriteLine("Scheme -- bits/int -- speed (mis)");

        foreach (ByteIntegerCODEC c in cm == CompressionMode.DELTA ? bcodecs : regbcodecs)
        {
            long decomptime = 0;
            long volumein = 0;
            long volumeout = 0;
            sbyte[][] compdata = new sbyte[postings.Count][];
            for (int k = 0; k < postings.Count; ++k)
            {
                int[] @in = postings[k];
                IntWrapper inpos = new IntWrapper(0);
                IntWrapper outpos = new IntWrapper(0);
                c.compress(@in, inpos, @in.Length, compbuffer,
                    outpos);
                int clength = outpos.get();
                inpos = new IntWrapper(0);
                outpos = new IntWrapper(0);
                c.uncompress(compbuffer, inpos, clength,
                    decompbuffer, outpos);
                volumein += @in.Length;
                volumeout += clength;

                if (outpos.get() != @in.Length)
                    throw new InvalidOperationException("bug");
                for (int z = 0; z < @in.Length; ++z)
                {
                    if (@in[z] != decompbuffer[z])
                    {
                        throw new InvalidOperationException(
                            "bug");
                    }
                }
                compdata[k] = Arrays
                    .copyOf(compbuffer, clength);
            }
            long bef = Port.System.nanoTime();
            foreach (sbyte[] cin in compdata)
            {
                IntWrapper inpos = new IntWrapper(0);
                IntWrapper outpos = new IntWrapper(0);
                c.uncompress(cin, inpos, cin.Length,
                    decompbuffer, outpos);
                if (inpos.get() != cin.Length)
                    throw new InvalidOperationException("bug");
            }
            long aft = Port.System.nanoTime();
            decomptime += aft - bef;
            double bitsPerInt = (volumeout * 8.0) / volumein;
            double decompressSpeed = (volumein * 1000.0) / decomptime;
            if (verbose)
                Console.WriteLine($"{c}\t\t{bitsPerInt:0.00}\t{decompressSpeed:0.00}");
        }
    }
}