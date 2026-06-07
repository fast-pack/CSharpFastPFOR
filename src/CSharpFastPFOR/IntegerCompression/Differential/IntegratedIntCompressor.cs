/*
 * This is a convenience class that wraps a codec to provide
 * a "friendly" API. It is useful to compress sorted integers.
 * If your integers are not sorted (not even nearly so), please
 * consider the IntCompressor class instead.
 *
 */

using Genbox.CSharpFastPFOR.IntegerCompression.Port;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Differential;

public class IntegratedIntCompressor(SkippableIntegratedIntegerCODEC c)
{
    /*
     * Constructor with default codec.
     */
    public IntegratedIntCompressor()
        : this(new SkippableIntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte()))
    {
    }

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
        compressed[0] = input.Length;
        IntWrapper outpos = new IntWrapper(1);
        IntWrapper initvalue = new IntWrapper(0);
        c.headlessCompress(input, new IntWrapper(0), input.Length, compressed, outpos, initvalue);
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
        int[] decompressed = new int[compressed[0]];
        IntWrapper inpos = new IntWrapper(1);
        c.headlessUncompress(compressed, inpos,
            compressed.Length - inpos.intValue(),
            decompressed, new IntWrapper(0),
            decompressed.Length, new IntWrapper(0));
        return decompressed;
    }
}