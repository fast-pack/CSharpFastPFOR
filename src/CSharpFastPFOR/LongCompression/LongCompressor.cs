using Genbox.CSharpFastPFOR.IntegerCompression;

namespace Genbox.CSharpFastPFOR.LongCompression;

/*
 * This is a convenience class that wraps a codec to provide
 * a "friendly" API.
 *
 * @author Benoit Lacelle
 */
public class LongCompressor(SkippableLongCODEC c)
{
    /*
     * Constructor with default codec.
     */
    public LongCompressor()
        : this(new SkippableLongComposition(new LongBinaryPacking(), new LongVariableByte()))
    {
    }

    /*
     * Compress an array and returns the compressed result as a new array.
     *
     * @param input array to be compressed
     * @return compressed array
     */
    public long[] compress(long[] input)
    {
        int maxCompressedLength = c.maxHeadlessCompressedLength(new IntWrapper(0), input.Length);
        long[] compressed = new long[maxCompressedLength + 1]; // +1 to store the length of the input
        // Store at index=0 the length of the input, hence enabling .headlessCompress
        compressed[0] = input.Length;
        IntWrapper outpos = new IntWrapper(1);
        c.headlessCompress(input, new IntWrapper(0), input.Length, compressed, outpos);
        long[] copy = new long[outpos.get()];
        Array.Copy(compressed, copy, copy.Length);
        compressed = copy;
        return compressed;
    }

    /*
     * Uncompress an array and returns the uncompressed result as a new array.
     *
     * @param compressed compressed array
     * @return uncompressed array
     */
    public long[] uncompress(long[] compressed)
    {
        // Read at index=0 the length of the input, hence enabling .headlessUncompress
        long[] decompressed = new long[(int)compressed[0]];
        IntWrapper inpos = new IntWrapper(1);
        c.headlessUncompress(compressed, inpos,
            compressed.Length - inpos.get(),
            decompressed, new IntWrapper(0),
            decompressed.Length);
        return decompressed;
    }
}