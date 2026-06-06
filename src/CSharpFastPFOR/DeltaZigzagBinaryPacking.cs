/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

/**
 * BinaryPacking with Delta+Zigzag Encoding.
 *
 * It encodes integers in blocks of 128 integers. For arrays containing
 * an arbitrary number of integers, you should use it in conjunction
 * with another CODEC:
 *
 *  <pre>IntegerCODEC ic = new Composition(new DeltaZigzagBinaryPacking(),
 *                      new DeltaZigzagVariableByte()).</pre>
 *
 * @author MURAOKA Taro http://github.com/koron
 */

namespace Genbox.CSharpFastPFOR;

public class DeltaZigzagBinaryPacking : IntegerCODEC
{
    private const int BLOCK_LENGTH = 128;

    public void compress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        inlength -= (inlength % BLOCK_LENGTH);
        if (inlength == 0)
        {
            return;
        }

        @out[outpos.get()] = inlength;
        outpos.increment();

        DeltaZigzagEncoding.Encoder ctx = new DeltaZigzagEncoding.Encoder(0);
        int[] work = new int[BLOCK_LENGTH];

        int op = outpos.get();
        int ip = inpos.get();
        int inPosLast = ip + inlength;
        for (; ip < inPosLast; ip += BLOCK_LENGTH)
        {
            ctx.encodeArray(@in, ip, BLOCK_LENGTH, work);
            int bits1 = Util.maxbits32(work, 0);
            int bits2 = Util.maxbits32(work, 32);
            int bits3 = Util.maxbits32(work, 64);
            int bits4 = Util.maxbits32(work, 96);
            @out[op++] = (bits1 << 24) | (bits2 << 16)
                                         | (bits3 << 8) | (bits4 << 0);
            op += pack(work, 0, @out, op, bits1);
            op += pack(work, 32, @out, op, bits2);
            op += pack(work, 64, @out, op, bits3);
            op += pack(work, 96, @out, op, bits4);
        }

        inpos.add(inlength);
        outpos.set(op);
    }

    public void uncompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        if (inlength == 0)
        {
            return;
        }

        int outLen = @in[inpos.get()];
        inpos.increment();

        DeltaZigzagEncoding.Decoder ctx = new DeltaZigzagEncoding.Decoder(0);
        int[] work = new int[BLOCK_LENGTH];

        int ip = inpos.get();
        int op = outpos.get();
        int outPosLast = op + outLen;
        for (; op < outPosLast; op += BLOCK_LENGTH)
        {
            int n = @in[ip++];
            ip += unpack(@in, ip, work, 0, (n >> 24) & 0x3F);
            ip += unpack(@in, ip, work, 32, (n >> 16) & 0x3F);
            ip += unpack(@in, ip, work, 64, (n >> 8) & 0x3F);
            ip += unpack(@in, ip, work, 96, (n >> 0) & 0x3F);
            ctx.decodeArray(work, 0, BLOCK_LENGTH, @out, op);
        }

        outpos.add(outLen);
        inpos.set(ip);
    }

    private static int pack(int[] inBuf, int inOff, int[] outBuf, int outOff, int validBits)
    {
        BitPacking.fastpackwithoutmask(inBuf, inOff, outBuf, outOff, validBits);
        return validBits;
    }

    private static int unpack(int[] inBuf, int inOff, int[] outBuf, int outOff, int validBits)
    {
        BitPacking.fastunpack(inBuf, inOff, outBuf, outOff, validBits);
        return validBits;
    }

    public override string ToString()
    {
        return nameof(DeltaZigzagBinaryPacking);
    }
}