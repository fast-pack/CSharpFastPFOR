





/**
 * This is a convenience class that wraps a codec to provide
 * a "friendly" API.
 *
 */

using Genbox.CSharpFastPFOR.Port;

namespace Genbox.CSharpFastPFOR.Differential;

public class IntegratedIntCompressor
{
    private SkippableIntegratedIntegerCODEC codec;

    /**
     * Constructor wrapping a codec.
     *
     * @param c the underlying codec
     */
    public IntegratedIntCompressor(SkippableIntegratedIntegerCODEC c)
    {
        codec = c;
    }

    /**
     * Constructor with default codec.
     */
    public IntegratedIntCompressor()
    {
        codec = new SkippableIntegratedComposition(new IntegratedBinaryPacking(), new IntegratedVariableByte());
    }

    /**
     * Compress an array and returns the compressed result as a new array.
     *
     * @param input array to be compressed
     * @return compressed array
     */
    public int[] compress(int[] input)
    {
        int[] compressed = new int[input.Length + 1024];
        compressed[0] = input.Length;
        IntWrapper outpos = new IntWrapper(1);
        IntWrapper initvalue = new IntWrapper(0);
        codec.headlessCompress(input, new IntWrapper(0),
            input.Length, compressed, outpos, initvalue);
        compressed = Arrays.copyOf(compressed, outpos.intValue());
        return compressed;
    }

    /**
     * Uncompress an array and returns the uncompressed result as a new array.
     *
     * @param compressed compressed array
     * @return uncompressed array
     */
    public int[] uncompress(int[] compressed)
    {
        int[] decompressed = new int[compressed[0]];
        IntWrapper inpos = new IntWrapper(1);
        codec.headlessUncompress(compressed, inpos,
            compressed.Length - inpos.intValue(),
            decompressed, new IntWrapper(0),
            decompressed.Length, new IntWrapper(0));
        return decompressed;
    }
}