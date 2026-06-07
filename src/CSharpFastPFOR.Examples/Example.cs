using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;

namespace Genbox.CSharpFastPFOR.Examples;

internal static class example
{
    public static void main(string[] args)
    {
        _ = args;
        superSimpleExample();
        unsortedExample();
        basicExample();
        basicExampleHeadless();
        advancedExample();
        headlessDemo();
    }

    private static void superSimpleExample()
    {
        IntegratedIntCompressor iic = new IntegratedIntCompressor();
        int[] data = new int[2342351];
        for (int k = 0; k < data.Length; ++k)
            data[k] = k;
        Console.WriteLine("Compressing " + data.Length + " integers using friendly interface");
        int[] compressed = iic.compress(data);
        int[] recov = iic.uncompress(compressed);
        Console.WriteLine("compressed from " + ((data.Length * 4) / 1024) + "KB to " + ((compressed.Length * 4) / 1024) + "KB");
        if (!Arrays.equals(recov, data)) throw new InvalidOperationException("bug");
    }

    private static void basicExample()
    {
        int[] data = new int[2342351];
        Console.WriteLine("Compressing " + data.Length + " integers in one go");
        // data should be sorted for best
        //results
        for (int k = 0; k < data.Length; ++k)
            data[k] = k;
        // Very important: the data is in sorted order!!! If not, you
        // will get very poor compression with IntegratedBinaryPacking,
        // you should use another CODEC.

        // next we compose a CODEC. Most of the processing
        // will be done with binary packing, and leftovers will
        // be processed using variable byte
        IntegratedComposition codec = new
            IntegratedComposition(
                new IntegratedBinaryPacking(),
                new IntegratedVariableByte());
        // output vector should be large enough...
        int[] compressed = new int[data.Length + 1024];
        // compressed might not be large enough in some cases
        // if you get java.lang.ArrayIndexOutOfBoundsException, try
        // allocating more memory

        /*
         *
         * compressing
         *
         */
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);
        codec.compress(data, inputoffset, data.Length, compressed, outputoffset);
        // got it!
        // inputoffset should be at data.length but outputoffset tells
        // us where we are...
        Console.WriteLine("compressed from " + ((data.Length * 4) / 1024) + "KB to " + ((outputoffset.intValue() * 4) / 1024) + "KB");
        // we can repack the data: (optional)
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        /*
         *
         * now uncompressing
         *
         * This assumes that we otherwise know how many integers
         * have been compressed, or we can bound it (e.g., you know that
         * will never need to decore more than 2000 integers).
         * See basicExampleHeadless for a
         * more general case where you can manually manage the compressed
         * array size.
         */
        int[] recovered = new int[data.Length];
        IntWrapper recoffset = new IntWrapper(0);
        codec.uncompress(compressed, new IntWrapper(0), compressed.Length, recovered, recoffset);
        if (Arrays.equals(data, recovered))
            Console.WriteLine("data is recovered without loss");
        else
            throw new InvalidOperationException("bug"); // could use assert
        Console.WriteLine();
    }

    /*
     * Like the basicExample, but we store the input array size manually.
     */
    private static void basicExampleHeadless()
    {
        int[] data = new int[2342351];
        Console.WriteLine("Compressing " + data.Length + " integers in one go using the headless approach");
        // data should be sorted for best
        // results
        for (int k = 0; k < data.Length; ++k)
            data[k] = k;
        // Very important: the data is in sorted order!!! If not, you
        // will get very poor compression with IntegratedBinaryPacking,
        // you should use another CODEC.

        // next we compose a CODEC. Most of the processing
        // will be done with binary packing, and leftovers will
        // be processed using variable byte
        SkippableIntegratedComposition codec = new SkippableIntegratedComposition(new IntegratedBinaryPacking(),
            new IntegratedVariableByte());
        int[] compressed = new int[codec.maxHeadlessCompressedLength(new IntWrapper(0), data.Length)];

        /*
         *
         * compressing
         *
         */
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(1);
        compressed[0] = data.Length; // we manually store how many integers
        codec.headlessCompress(data, inputoffset, data.Length, compressed, outputoffset, new IntWrapper(0));
        // got it!
        // inputoffset should be at data.length but outputoffset tells
        // us where we are...
        Console.WriteLine(
            "compressed from " + ((data.Length * 4) / 1024) + "KB to " + ((outputoffset.intValue() * 4) / 1024) + "KB");
        // we can repack the data: (optional)
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        /*
         *
         * now uncompressing
         *
         */
        int howmany = compressed[0]; // we manually stored the number of
        // compressed integers
        int[] recovered = new int[howmany];
        IntWrapper recoffset = new IntWrapper(0);
        codec.headlessUncompress(compressed, new IntWrapper(1), compressed.Length, recovered, recoffset, howmany, new IntWrapper(0));
        if (Arrays.equals(data, recovered))
            Console.WriteLine("data is recovered without loss");
        else
            throw new InvalidOperationException("bug"); // could use assert
        Console.WriteLine();
    }

    /*
     * This is an example to show you can compress unsorted integers
     * as long as most are small.
     */
    private static void unsortedExample()
    {
        const int N = 1333333;
        int[] data = new int[N];
        // initialize the data (most will be small
        for (int k = 0; k < N; k += 1) data[k] = 3;
        // throw some larger values
        for (int k = 0; k < N; k += 5) data[k] = 100;
        for (int k = 0; k < N; k += 533) data[k] = 10000;
        int[] compressed = new int[N + 1024]; // could need more
        Composition codec = new
            Composition(
                new FastPFOR(),
                new VariableByte());
        // compressing
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);
        codec.compress(data, inputoffset, data.Length, compressed, outputoffset);
        Console.WriteLine("compressed unsorted integers from " + ((data.Length * 4) / 1024) + "KB to " + ((outputoffset.intValue() * 4) / 1024) + "KB");
        // we can repack the data: (optional)
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        int[] recovered = new int[N];
        IntWrapper recoffset = new IntWrapper(0);
        codec.uncompress(compressed, new IntWrapper(0), compressed.Length, recovered, recoffset);
        if (Arrays.equals(data, recovered))
            Console.WriteLine("data is recovered without loss");
        else
            throw new InvalidOperationException("bug"); // could use assert
        Console.WriteLine();
    }

    /*
     * This is like the basic example, but we
     * show how to process larger arrays in chunks.
     *
     * Some of this code was written by Pavel Klinov.
     */
    private static void advancedExample()
    {
        int TotalSize = 2342351; // some arbitrary number
        int ChunkSize = 16384; // size of each chunk, choose a multiple of 128
        Console.WriteLine("Compressing " + TotalSize + " integers using chunks of " + ChunkSize + " integers (" + ((ChunkSize * 4) / 1024) + "KB)");
        Console.WriteLine("(It is often better for applications to work in chunks fitting in CPU cache.)");
        int[] data = new int[TotalSize];
        // data should be sorted for best
        //results
        for (int k = 0; k < data.Length; ++k)
            data[k] = k;
        // next we compose a CODEC. Most of the processing
        // will be done with binary packing, and leftovers will
        // be processed using variable byte, using variable byte
        // only for the last chunk!
        IntegratedBinaryPacking regularcodec = new
            IntegratedBinaryPacking();
        IntegratedVariableByte ivb = new IntegratedVariableByte();
        IntegratedComposition lastcodec = new
            IntegratedComposition(regularcodec, ivb);
        // output vector should be large enough...
        int[] compressed = new int[TotalSize + 1024];

        /*
         *
         * compressing
         *
         */
        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);
        for (int k = 0; k < TotalSize / ChunkSize; ++k)
            regularcodec.compress(data, inputoffset, ChunkSize, compressed, outputoffset);
        lastcodec.compress(data, inputoffset, TotalSize % ChunkSize, compressed, outputoffset);
        // got it!
        // inputoffset should be at data.length but outputoffset tells
        // us where we are...
        Console.WriteLine("compressed from " + ((data.Length * 4) / 1024) + "KB to " + ((outputoffset.intValue() * 4) / 1024) + "KB");
        // we can repack the data:
        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        /*
         *
         * now uncompressing
         *
         * We are *not* assuming that the original array length is known, however
         * we assume that the chunk size (ChunkSize) is known.
         *
         */
        int[] recovered = new int[ChunkSize];
        IntWrapper compoff = new IntWrapper(0);
        int currentpos = 0;

        while (compoff.get() < compressed.Length)
        {
            IntWrapper recoffset = new IntWrapper(0);
            regularcodec.uncompress(compressed, compoff, compressed.Length - compoff.get(), recovered, recoffset);

            if (recoffset.get() < ChunkSize)
            {
                // last chunk detected
                ivb.uncompress(compressed, compoff, compressed.Length - compoff.get(), recovered, recoffset);
            }
            for (int i = 0; i < recoffset.get(); ++i)
            {
                if (data[currentpos + i] != recovered[i])
                    throw new InvalidOperationException("bug"); // could use assert
            }
            currentpos += recoffset.get();
        }
        Console.WriteLine("data is recovered without loss");
        Console.WriteLine();
    }

    /*
     *  Demo of the headless approach where we must supply the array length
     */
    private static void headlessDemo()
    {
        Console.WriteLine("Compressing arrays with minimal header...");
        int[] uncompressed1 = [1, 2, 1, 3, 1];
        int[] uncompressed2 = [3, 2, 4, 6, 1];

        SkippableComposition codec = new SkippableComposition(new BinaryPacking(), new VariableByte());

        int maxCompressedLength = codec.maxHeadlessCompressedLength(new IntWrapper(0), uncompressed1.Length)
                                  + codec.maxHeadlessCompressedLength(new IntWrapper(0), uncompressed2.Length);
        int[] compressed = new int[maxCompressedLength];

        // compressing
        IntWrapper outPos = new IntWrapper();

        IntWrapper previous = new IntWrapper();

        codec.headlessCompress(uncompressed1, new IntWrapper(), uncompressed1.Length, compressed, outPos);
        int length1 = outPos.get() - previous.get();
        previous = new IntWrapper(outPos.get());
        codec.headlessCompress(uncompressed2, new IntWrapper(), uncompressed2.Length, compressed, outPos);
        int length2 = outPos.get() - previous.get();

        compressed = Arrays.copyOf(compressed, length1 + length2);
        Console.WriteLine("compressed unsorted integers from " + (uncompressed1.Length * 4) + "B to " + (length1 * 4) + "B");
        Console.WriteLine("compressed unsorted integers from " + (uncompressed2.Length * 4) + "B to " + (length2 * 4) + "B");
        Console.WriteLine("Total compressed output " + compressed.Length);

        int[] recovered1 = new int[uncompressed1.Length];
        int[] recovered2 = new int[uncompressed1.Length];
        IntWrapper inPos = new IntWrapper();
        Console.WriteLine("Decoding first array starting at pos = " + inPos);
        codec.headlessUncompress(compressed, inPos, compressed.Length, recovered1, new IntWrapper(0), uncompressed1.Length);
        Console.WriteLine("Decoding second array starting at pos = " + inPos);
        codec.headlessUncompress(compressed, inPos, compressed.Length, recovered2, new IntWrapper(0), uncompressed2.Length);
        if (!Arrays.equals(uncompressed1, recovered1)) throw new InvalidOperationException("First array does not match.");
        if (!Arrays.equals(uncompressed2, recovered2)) throw new InvalidOperationException("Second array does not match.");
        Console.WriteLine("The arrays match, your code is probably ok.");
    }
}