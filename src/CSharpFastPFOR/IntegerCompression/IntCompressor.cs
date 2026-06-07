/*
 * This is a convenience class that wraps a codec to provide
 * a "friendly" API.
 *
 */

using Genbox.CSharpFastPFOR.IntegerCompression.Port;

namespace Genbox.CSharpFastPFOR.IntegerCompression;

public class IntCompressor(SkippableIntegerCODEC c)
{
    /*
     * Constructor wrapping a codec.
     *
     * @param c the underlying codec
     */
    /*
     * Constructor with default codec.
     */
    public IntCompressor() : this(new SkippableComposition(new BinaryPacking(),
        new VariableByte())) {}

    /*
     * Compress an array and returns the compressed result as a new array.
     *
     * @param input array to be compressed
     * @return compressed array
     */
    public int[] compress(int[] input)
    {
        int maxCompressedLength = c.maxHeadlessCompressedLength(new IntWrapper(0), input.Length);
        int[] compressed = new int[maxCompressedLength + 1]; // +1 to store the length of the input
        // Store at index=0 the length of the input, hence enabling .headlessCompress
        compressed[0] = input.Length;
        IntWrapper outpos = new IntWrapper(1);
        c.headlessCompress(input, new IntWrapper(0), input.Length, compressed, outpos);
        compressed = Arrays.copyOf(compressed, outpos.intValue());
        return compressed;
    }

    /*
     * Uncompress an array and returns the uncompressed result as a new array.
     *
     * @param compressed compressed array
     * @return uncompressed array
     */
    public int[] uncompress(int[] compressed)
    {
        // Read at index=0 the length of the input, hence enabling .headlessUncompress
        int[] decompressed = new int[compressed[0]];
        IntWrapper inpos = new IntWrapper(1);
        c.headlessUncompress(compressed, inpos,
            compressed.Length - inpos.intValue(),
            decompressed, new IntWrapper(0),
            decompressed.Length);
        return decompressed;
    }
}