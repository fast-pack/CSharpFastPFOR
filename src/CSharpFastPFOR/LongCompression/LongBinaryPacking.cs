using Genbox.CSharpFastPFOR.IntegerCompression;

namespace Genbox.CSharpFastPFOR.LongCompression;

/*
 * Scheme  based on a commonly used idea: can be extremely fast.
 * It encodes integers in blocks of 64 longs. For arrays containing
 * an arbitrary number of longs, you should use it in conjunction
 * with another CODEC:
 * <pre>
 * LongCODEC ic =
 * new Composition(new LongBinaryPacking(), new LongVariableByte()).
 * </pre>
 * Note that this does not use differential coding: if you are working on sorted
 * lists, you must compute the deltas separately.
 * <p>
 * For details, please see {@link BinaryPacking}
 * </p>
 * @author Benoit Lacelle
 */
public sealed class LongBinaryPacking : LongCODEC, SkippableLongCODEC
{
    public const int BLOCK_SIZE = 64;
    private const int MAX_BIT_WIDTH = sizeof(long) * 8;

    public void compress(long[] @in, IntWrapper inpos, int inlength, long[] @out, IntWrapper outpos)
    {
        inlength = Util.greatestMultiple(inlength, BLOCK_SIZE);
        if (inlength == 0)
            return;
        @out[outpos.get()] = inlength;
        outpos.increment();
        headlessCompress(@in, inpos, inlength, @out, outpos);
    }

    public void uncompress(long[] @in, IntWrapper inpos, int inlength, long[] @out, IntWrapper outpos)
    {
        if (inlength == 0)
            return;
        int outlength = (int)@in[inpos.get()];
        inpos.increment();
        headlessUncompress(@in, inpos, inlength, @out, outpos, outlength);
    }

    public void headlessCompress(long[] @in, IntWrapper inpos, int inlength, long[] @out, IntWrapper outpos)
    {
        inlength = Util.greatestMultiple(inlength, BLOCK_SIZE);
        int tmpoutpos = outpos.get();
        int s = inpos.get();
        // Compress by block of 8 * 64 longs as much as possible
        for (; (s + (BLOCK_SIZE * 8)) - 1 < inpos.get() + inlength; s += BLOCK_SIZE * 8)
        {
            // maxbits can be anything between 0 and 64 included: expressed within a byte (1 << 6)
            int mbits1 = maxbits(@in, s + (0 * BLOCK_SIZE), BLOCK_SIZE);
            int mbits2 = maxbits(@in, s + (1 * BLOCK_SIZE), BLOCK_SIZE);
            int mbits3 = maxbits(@in, s + (2 * BLOCK_SIZE), BLOCK_SIZE);
            int mbits4 = maxbits(@in, s + (3 * BLOCK_SIZE), BLOCK_SIZE);
            int mbits5 = maxbits(@in, s + (4 * BLOCK_SIZE), BLOCK_SIZE);
            int mbits6 = maxbits(@in, s + (5 * BLOCK_SIZE), BLOCK_SIZE);
            int mbits7 = maxbits(@in, s + (6 * BLOCK_SIZE), BLOCK_SIZE);
            int mbits8 = maxbits(@in, s + (7 * BLOCK_SIZE), BLOCK_SIZE);
            // The first long expressed the maxbits for the 8 buckets
            @out[tmpoutpos++] = ((long)mbits1 << 56) | ((long)mbits2 << 48) | ((long)mbits3 << 40) | ((long)mbits4 << 32) | ((long)mbits5 << 24) | ((long)mbits6 << 16) | ((long)mbits7 << 8) | (uint)mbits8;
            LongBitPacking.fastpackwithoutmask(@in, s + (0 * BLOCK_SIZE), @out, tmpoutpos, mbits1);
            tmpoutpos += mbits1;
            LongBitPacking.fastpackwithoutmask(@in, s + (1 * BLOCK_SIZE), @out, tmpoutpos, mbits2);
            tmpoutpos += mbits2;
            LongBitPacking.fastpackwithoutmask(@in, s + (2 * BLOCK_SIZE), @out, tmpoutpos, mbits3);
            tmpoutpos += mbits3;
            LongBitPacking.fastpackwithoutmask(@in, s + (3 * BLOCK_SIZE), @out, tmpoutpos, mbits4);
            tmpoutpos += mbits4;
            LongBitPacking.fastpackwithoutmask(@in, s + (4 * BLOCK_SIZE), @out, tmpoutpos, mbits5);
            tmpoutpos += mbits5;
            LongBitPacking.fastpackwithoutmask(@in, s + (5 * BLOCK_SIZE), @out, tmpoutpos, mbits6);
            tmpoutpos += mbits6;
            LongBitPacking.fastpackwithoutmask(@in, s + (6 * BLOCK_SIZE), @out, tmpoutpos, mbits7);
            tmpoutpos += mbits7;
            LongBitPacking.fastpackwithoutmask(@in, s + (7 * BLOCK_SIZE), @out, tmpoutpos, mbits8);
            tmpoutpos += mbits8;
        }
        // Then we compress up to 7 blocks of 64 longs
        for (; s < inpos.get() + inlength; s += BLOCK_SIZE)
        {
            int mbits = maxbits(@in, s, BLOCK_SIZE);
            @out[tmpoutpos++] = mbits;
            LongBitPacking.fastpackwithoutmask(@in, s, @out, tmpoutpos, mbits);
            tmpoutpos += mbits;
        }
        inpos.add(inlength);
        outpos.set(tmpoutpos);
    }

    public void headlessUncompress(long[] @in, IntWrapper inpos, int inlength, long[] @out, IntWrapper outpos, int num)
    {
        int outlength = Util.greatestMultiple(num, BLOCK_SIZE);
        int tmpinpos = inpos.get();
        int s = outpos.get();
        for (; (s + (BLOCK_SIZE * 8)) - 1 < outpos.get() + outlength; s += BLOCK_SIZE * 8)
        {
            int mbits1 = (int)((ulong)@in[tmpinpos] >> 56);
            int mbits2 = (int)(((ulong)@in[tmpinpos] >> 48) & 0xFF);
            int mbits3 = (int)(((ulong)@in[tmpinpos] >> 40) & 0xFF);
            int mbits4 = (int)(((ulong)@in[tmpinpos] >> 32) & 0xFF);
            int mbits5 = (int)(((ulong)@in[tmpinpos] >> 24) & 0xFF);
            int mbits6 = (int)(((ulong)@in[tmpinpos] >> 16) & 0xFF);
            int mbits7 = (int)(((ulong)@in[tmpinpos] >> 8) & 0xFF);
            int mbits8 = (int)(@in[tmpinpos] & 0xFF);
            ++tmpinpos;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (0 * BLOCK_SIZE), mbits1);
            tmpinpos += mbits1;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (1 * BLOCK_SIZE), mbits2);
            tmpinpos += mbits2;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (2 * BLOCK_SIZE), mbits3);
            tmpinpos += mbits3;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (3 * BLOCK_SIZE), mbits4);
            tmpinpos += mbits4;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (4 * BLOCK_SIZE), mbits5);
            tmpinpos += mbits5;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (5 * BLOCK_SIZE), mbits6);
            tmpinpos += mbits6;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (6 * BLOCK_SIZE), mbits7);
            tmpinpos += mbits7;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s + (7 * BLOCK_SIZE), mbits8);
            tmpinpos += mbits8;
        }
        for (; s < outpos.get() + outlength; s += BLOCK_SIZE)
        {
            int mbits = (int)@in[tmpinpos];
            ++tmpinpos;
            LongBitPacking.fastunpack(@in, tmpinpos, @out, s, mbits);
            tmpinpos += mbits;
        }
        outpos.add(outlength);
        inpos.set(tmpinpos);
    }

    public int maxHeadlessCompressedLength(IntWrapper compressedPositions, int inlength)
    {
        int blockCount = inlength / BLOCK_SIZE;
        int headersSizeInLongs = (blockCount / sizeof(long)) + (blockCount % sizeof(long));
        int blocksSizeInLongs = blockCount * MAX_BIT_WIDTH;
        compressedPositions.add(blockCount * BLOCK_SIZE);
        return headersSizeInLongs + blocksSizeInLongs;
    }

    private static int maxbits(long[] values, int pos, int length)
    {
        long mask = 0;
        for (int k = pos; k < pos + length; ++k)
            mask |= values[k];
        int bits = 0;
        for (ulong value = (ulong)mask; value != 0; value >>= 1)
            ++bits;
        return bits;
    }

    public override string ToString() => nameof(LongBinaryPacking);
}