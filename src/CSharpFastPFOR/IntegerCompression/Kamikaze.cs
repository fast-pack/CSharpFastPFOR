using Genbox.CSharpFastPFOR.Kamikaze;

namespace Genbox.CSharpFastPFOR.IntegerCompression;

/*
 * IntegerCODEC wrapper for Kamikaze's PForDelta.
 *
 * Note: this class is only included for speed benchmarks.
 * It is not recommended. Use at your own risks.
 *
 * @author Matteo Catena
 */
public class Kamikaze : SkippableIntegerCODEC, IntegerCODEC
{
    private readonly int BLOCK_SIZE = 128;

    public void compress(int[] @in,
        IntWrapper inpos,
        int inlength,
        int[] @out,
        IntWrapper outpos)
    {
        inlength = Util.greatestMultiple(inlength, BLOCK_SIZE);
        if (inlength == 0)
            return;
        @out[outpos.get()] = inlength;
        outpos.increment();
        headlessCompress(@in, inpos, inlength, @out, outpos);
    }

    public void uncompress(int[] @in,
        IntWrapper inpos,
        int inlength,
        int[] @out,
        IntWrapper outpos)
    {
        if (inlength == 0)
            return;
        int outlength = @in[inpos.get()];
        inpos.increment();
        headlessUncompress(@in, inpos, inlength, @out, outpos, outlength);
    }

    public void headlessCompress(int[] @in,
        IntWrapper inpos,
        int inlength,
        int[] @out,
        IntWrapper outpos)
    {
        inlength = Util.greatestMultiple(inlength, BLOCK_SIZE);
        if (inlength > 0)
        {
            int[] out2 = PForDelta.compressOneBlockOpt(@in, inlength);
            inpos.add(inlength);
            Array.Copy(out2, 0, @out, outpos.get(), out2.Length);
            outpos.add(out2.Length);
        }
    }

    public void headlessUncompress(int[] @in,
        IntWrapper inpos,
        int inlength,
        int[] @out,
        IntWrapper outpos,
        int num)
    {
        num = Util.greatestMultiple(num, BLOCK_SIZE);
        if (num > 0)
        {
            int d = PForDelta.decompressOneBlock(@out, @in, num);
            inpos.add(d / 32);
            outpos.add(num);
        }
    }

    public int maxHeadlessCompressedLength(IntWrapper compressedPositions, int inlength) => throw new NotSupportedException("Calculating the max compressed length is not supported yet.");

    public override string ToString() => "Kamikaze's PForDelta";
}