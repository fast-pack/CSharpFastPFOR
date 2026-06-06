/**
 * This will run benchmarks using a set of posting lists stored as CSV files.
 *
 * @author lemire
 *
 */

using Genbox.CSharpFastPFOR.Differential;
using Genbox.CSharpFastPFOR.Port;

namespace Genbox.CSharpFastPFOR.Benchmarks;

public enum Format
{
    ONEARRAYPERLINE,
    ONEARRAYPERFILE,
    ONEINTPERLINE
}

public enum CompressionMode
{
    AS_IS,
    DELTA
}

public static class BenchmarkCSV
{
    private static IntegratedIntegerCODEC[] codecs = [new IntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte())];
    private static IntegratedByteIntegerCODEC[] bcodecs = [new IntegratedVariableByte()];
    private static IntegerCODEC[] regcodecs =
    [
        new Composition(new FastPFOR128(), new VariableByte()),
        new Composition(new FastPFOR(), new VariableByte()),
        new Composition(new BinaryPacking(), new VariableByte())
    ];
    private static ByteIntegerCODEC[] regbcodecs = [new VariableByte()];

    public static List<int[]> loadIntegers(string filename, Format f)
    {
        int misparsed = 0;
        if (f == Format.ONEARRAYPERLINE)
        {
            List<int[]> answer = [];

            foreach (string s in File.ReadLines(filename))
            {
                string[] numbers = s.Split('[', ',', ' ', ';', ']'); // that's slow
                int[] a = new int[numbers.Length];
                for (int k = 0; k < numbers.Length; ++k)
                {
                    try
                    {
                        a[k] = int.Parse(numbers[k].Trim());
                    }
                    catch (Exception nfe)
                    {
                        if (misparsed == 0)
                            Console.WriteLine(nfe.Message);

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
                string[] numbers = s.Split('[', ',', ' ', ';', ']'); // that's slow
                for (int k = 0; k < numbers.Length; ++k)
                {
                    try
                    {
                        answer.Add(int.Parse(numbers[k].Trim()));
                    }
                    catch (Exception nfe)
                    {
                        if (misparsed == 0)
                            Console.WriteLine(nfe.Message);
                        ++misparsed;
                    }
                }
            }
            int[] actualanswer = new int[answer.Count];
            for (int i = 0; i < answer.Count; ++i)
                actualanswer[i] = answer[i];
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
                    answer.Add(int.Parse(s.Trim()));
                }
                catch (Exception nfe)
                {
                    if (misparsed == 0)
                        Console.WriteLine(nfe.Message);
                    ++misparsed;
                }
            }
            int[] actualanswer = new int[answer.Count];
            for (int i = 0; i < answer.Count; ++i)
                actualanswer[i] = answer[i];
            List<int[]> wrap =
            [
                actualanswer
            ];

            if (misparsed > 0)
                Console.WriteLine("Failed to parse " + misparsed + " entries");
            return wrap;
        }
    }

    public static void bench(List<int[]> postings, CompressionMode cm, bool verbose)
    {
        int maxlength = 0;
        foreach (int[] x in postings)
            if (maxlength < x.Length)
                maxlength = x.Length;
        if (verbose)
            Console.WriteLine("Max array length: " + maxlength);
        int[] compbuffer = new int[2 * maxlength + 1024];
        int[] decompbuffer = new int[maxlength];
        if (verbose)
            Console.WriteLine("Scheme -- bits/int -- speed (mis)");

        foreach (IntegerCODEC c in (cm == CompressionMode.DELTA ? codecs : regcodecs))
        {
            long bef = 0;
            long aft = 0;
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
                    throw new Exception("bug");
                for (int z = 0; z < @in.Length; ++z)
                    if (@in[z] != decompbuffer[z])
                        throw new Exception(
                            "bug");
                compdata[k] = Arrays
                    .copyOf(compbuffer, clength);
            }

            bef = Port.System.nanoTime();
            foreach (int[] cin in compdata)
            {
                IntWrapper inpos = new IntWrapper(0);
                IntWrapper outpos = new IntWrapper(0);
                c.uncompress(cin, inpos, cin.Length,
                    decompbuffer, outpos);
                if (inpos.get() != cin.Length)
                    throw new Exception("bug");
            }
            aft = Port.System.nanoTime();
            decomptime += (aft - bef);
            double bitsPerInt = volumeout * 32.0 / volumein;
            double decompressSpeed = volumein * 1000.0 / (decomptime);
            if (verbose)
                Console.WriteLine(c + "\t" + string.Format("\t{0:0.00}\t{1:0.00}", bitsPerInt, decompressSpeed));
        }
    }

    public static void bytebench(List<int[]> postings, CompressionMode cm, bool verbose)
    {
        int maxlength = 0;
        foreach (int[] x in postings)
            if (maxlength < x.Length)
                maxlength = x.Length;
        if (verbose)
            Console.WriteLine("Max array length: " + maxlength);
        sbyte[] compbuffer = new sbyte[6 * (maxlength + 1024)];
        int[] decompbuffer = new int[maxlength];
        if (verbose)
            Console.WriteLine("Scheme -- bits/int -- speed (mis)");

        foreach (ByteIntegerCODEC c in (cm == CompressionMode.DELTA ? bcodecs : regbcodecs))
        {
            long bef = 0;
            long aft = 0;
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
                    throw new Exception("bug");
                for (int z = 0; z < @in.Length; ++z)
                    if (@in[z] != decompbuffer[z])
                        throw new Exception(
                            "bug");
                compdata[k] = Arrays
                    .copyOf(compbuffer, clength);
            }
            bef = Port.System.nanoTime();
            foreach (sbyte[] cin in compdata)
            {
                IntWrapper inpos = new IntWrapper(0);
                IntWrapper outpos = new IntWrapper(0);
                c.uncompress(cin, inpos, cin.Length,
                    decompbuffer, outpos);
                if (inpos.get() != cin.Length)
                    throw new Exception("bug");
            }
            aft = Port.System.nanoTime();
            decomptime += (aft - bef);
            double bitsPerInt = volumeout * 8.0 / volumein;
            double decompressSpeed = volumein * 1000.0 / (decomptime);
            if (verbose)
                Console.WriteLine(c + "\t" + string.Format("\t{0:0.00}\t{1:0.00}", bitsPerInt, decompressSpeed));
        }
    }
}