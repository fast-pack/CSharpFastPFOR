/*
 * This is code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */
using Genbox.CSharpFastPFOR.IntegerCompression;

namespace Genbox.CSharpFastPFOR.LongCompression;

/*
 * Helper class to compose schemes.
 *
 * @author Benoit Lacelle
 */
public class SkippableLongComposition(SkippableLongCODEC f1, SkippableLongCODEC f2) : SkippableLongCODEC
{
    /*
     * Compose a scheme from a first one (f1) and a second one (f2). The first
     * one is called first and then the second one tries to compress whatever
     * remains from the first run.
     *
     * By convention, the first scheme should be such that if, during decoding,
     * a 32-bit zero is first encountered, then there is no output.
     *
     * @param f1
     * first codec
     * @param f2
     * second codec
     */
    public void headlessCompress(long[] @in,
        IntWrapper inpos,
        int inlength,
        long[] @out,
        IntWrapper outpos)
    {
        int init = inpos.get();
        int outposInit = outpos.get();
        f1.headlessCompress(@in, inpos, inlength, @out, outpos);
        if (outpos.get() == outposInit)
        {
            @out[outposInit] = 0;
            outpos.increment();
        }
        inlength -= inpos.get() - init;
        f2.headlessCompress(@in, inpos, inlength, @out, outpos);
    }

    public void headlessUncompress(long[] @in,
        IntWrapper inpos,
        int inlength,
        long[] @out,
        IntWrapper outpos,
        int num)
    {
        int init = inpos.get();
        int outposInit = outpos.get();

        f1.headlessUncompress(@in, inpos, inlength, @out, outpos, num);
        if (inpos.get() == init)
            inpos.increment();
        inlength -= inpos.get() - init;
        num -= outpos.get() - outposInit;
        f2.headlessUncompress(@in, inpos, inlength, @out, outpos, num);
    }

    public int maxHeadlessCompressedLength(IntWrapper compressedPositions, int inlength)
    {
        int init = compressedPositions.get();
        int maxLength = f1.maxHeadlessCompressedLength(compressedPositions, inlength);
        maxLength += 1; // Add +1 for the potential F2 header. Question: is this header actually needed in the headless version?
        inlength -= compressedPositions.get() - init;
        maxLength += f2.maxHeadlessCompressedLength(compressedPositions, inlength);
        return maxLength;
    }

    public override string ToString() => string.Concat(f1, "+", f2);
}