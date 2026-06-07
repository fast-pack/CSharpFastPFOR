/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using Genbox.CSharpFastPFOR.IntegerCompression;

namespace Genbox.CSharpFastPFOR.LongCompression;

/*
 * @author Benoit lacelle
 */
public sealed class LongJustCopy : LongCODEC, SkippableLongCODEC
{
    public void uncompress(long[] @in,
        IntWrapper inpos,
        int inlength,
        long[] @out,
        IntWrapper outpos)
    {
        headlessUncompress(@in, inpos, inlength, @out, outpos, inlength);
    }

    public void compress(long[] @in,
        IntWrapper inpos,
        int inlength,
        long[] @out,
        IntWrapper outpos)
    {
        headlessCompress(@in, inpos, inlength, @out, outpos);
    }

    public void headlessCompress(long[] @in,
        IntWrapper inpos,
        int inlength,
        long[] @out,
        IntWrapper outpos)
    {
        Array.Copy(@in, inpos.get(), @out, outpos.get(), inlength);
        inpos.add(inlength);
        outpos.add(inlength);
    }

    public void headlessUncompress(long[] @in,
        IntWrapper inpos,
        int inlength,
        long[] @out,
        IntWrapper outpos,
        int num)
    {
        Array.Copy(@in, inpos.get(), @out, outpos.get(), num);
        inpos.add(num);
        outpos.add(num);
    }

    public int maxHeadlessCompressedLength(IntWrapper compressedPositions, int inlength)
    {
        compressedPositions.add(inlength);
        return inlength;
    }

    public override string ToString() => nameof(LongJustCopy);
}