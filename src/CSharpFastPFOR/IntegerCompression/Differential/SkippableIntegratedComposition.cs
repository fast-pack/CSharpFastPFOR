/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

/*
 * Helper class to compose schemes.
 *
 * @author Daniel Lemire
 */
namespace Genbox.CSharpFastPFOR.IntegerCompression.Differential;

public class SkippableIntegratedComposition(SkippableIntegratedIntegerCODEC f1, SkippableIntegratedIntegerCODEC f2) : SkippableIntegratedIntegerCODEC
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
    public void headlessCompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos, IntWrapper initvalue)
    {
        if (inlength == 0)
            return;
        int init = inpos.get();
        int outposInit = outpos.get();
        f1.headlessCompress(@in, inpos, inlength, @out, outpos, initvalue);
        if (outpos.get() == outposInit)
        {
            @out[outposInit] = 0;
            outpos.increment();
        }
        inlength -= inpos.get() - init;
        f2.headlessCompress(@in, inpos, inlength, @out, outpos, initvalue);
    }

    public void headlessUncompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos, int num, IntWrapper initvalue)
    {
        if (inlength == 0)
            return;
        int init = inpos.get();
        int outposInit = outpos.get();
        f1.headlessUncompress(@in, inpos, inlength, @out, outpos, num, initvalue);
        if (inpos.get() == init)
            inpos.increment();
        inlength -= inpos.get() - init;
        num -= outpos.get() - outposInit;
        f2.headlessUncompress(@in, inpos, inlength, @out, outpos, num, initvalue);
    }

    public int maxHeadlessCompressedLength(IntWrapper compressedPositions, int inlength)
    {
        int init = compressedPositions.get();
        int maxLength = f1.maxHeadlessCompressedLength(compressedPositions, inlength);
        maxLength += 1;
        inlength -= compressedPositions.get() - init;
        maxLength += f2.maxHeadlessCompressedLength(compressedPositions, inlength);
        return maxLength;
    }

    public override string ToString() => string.Concat(f1, " + ", f2);
}