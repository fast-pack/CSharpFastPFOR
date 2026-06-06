/**
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

/**
 * BinaryPacking over XOR differential.
 *
 * <pre>IntegratedIntegerCODEC is =
 * new Composition(new XorBinaryPacking(), new VariableByte())</pre>
 *
 * @author MURAOKA Taro http://github.com/koron
 */

using Genbox.CSharpFastPFOR.Port;

namespace Genbox.CSharpFastPFOR.Differential;

public class XorBinaryPacking : IntegratedIntegerCODEC
{
    private const int BLOCK_LENGTH = 128;

    public void compress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        inlength = inlength - (inlength % BLOCK_LENGTH);
        if (inlength == 0)
            return;

        @out[outpos.get()] = inlength;
        outpos.increment();

        int context = 0;
        int[] work = new int[32];

        int op = outpos.get();
        int ip = inpos.get();
        int inPosLast = ip + inlength;
        for (; ip < inPosLast; ip += BLOCK_LENGTH)
        {
            int bits1 = xorMaxBits(@in, ip + 0, 32, context);
            int bits2 = xorMaxBits(@in, ip + 32, 32,
                @in[ip + 31]);
            int bits3 = xorMaxBits(@in, ip + 64, 32,
                @in[ip + 63]);
            int bits4 = xorMaxBits(@in, ip + 96, 32,
                @in[ip + 95]);
            @out[op++] = (bits1 << 24) | (bits2 << 16)
                                         | (bits3 << 8) | (bits4 << 0);
            op += xorPack(@in, ip + 0, @out, op, bits1,
                context, work);
            op += xorPack(@in, ip + 32, @out, op, bits2,
                @in[ip + 31], work);
            op += xorPack(@in, ip + 64, @out, op, bits3,
                @in[ip + 63], work);
            op += xorPack(@in, ip + 96, @out, op, bits4,
                @in[ip + 95], work);
            context = @in[ip + 127];
        }

        inpos.add(inlength);
        outpos.set(op);
    }

    public void uncompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        if (inlength == 0)
            return;

        int outLen = @in[inpos.get()];
        inpos.increment();

        int context = 0;
        int[] work = new int[32];

        int ip = inpos.get();
        int op = outpos.get();
        int outPosLast = op + outLen;
        for (; op < outPosLast; op += BLOCK_LENGTH)
        {
            int bits1 = @in[ip] >>> 24;
            int bits2 = @in[ip] >>> 16 & 0xFF;
            int bits3 = @in[ip] >>> 8 & 0xFF;
            int bits4 = @in[ip] >>> 0 & 0xFF;
            ++ip;
            ip += xorUnpack(@in, ip, @out, op + 0, bits1,
                context, work);
            ip += xorUnpack(@in, ip, @out, op + 32, bits2,
                @out[op + 31], work);
            ip += xorUnpack(@in, ip, @out, op + 64, bits3,
                @out[op + 63], work);
            ip += xorUnpack(@in, ip, @out, op + 96, bits4,
                @out[op + 95], work);
            context = @out[op + 127];
        }

        outpos.add(outLen);
        inpos.set(ip);
    }

    private static int xorMaxBits(int[] buf, int offset, int length, int context)
    {
        int mask = buf[offset] ^ context;
        int M = offset + length;
        for (int i = offset + 1, prev = offset; i < M; ++i, ++prev)
        {
            mask |= buf[i] ^ buf[prev];
        }

        return 32 - Integer.numberOfLeadingZeros(mask);
    }

    private static int xorPack(int[] inBuf, int inOff, int[] outBuf, int outOff, int validBits, int context, int[] work)
    {
        work[0] = inBuf[inOff] ^ context;
        for (int i = 1, p = inOff + 1; i < 32; ++i, ++p)
        {
            work[i] = inBuf[p] ^ inBuf[p - 1];
        }
        BitPacking.fastpackwithoutmask(work, 0, outBuf, outOff,
            validBits);

        return validBits;
    }

    private static int xorUnpack(int[] inBuf, int inOff, int[] outBuf, int outOff, int validBits, int context, int[] work)
    {
        BitPacking.fastunpack(inBuf, inOff, work, 0, validBits);
        outBuf[outOff] = context = work[0] ^ context;
        for (int i = 1, p = outOff + 1; i < 32; ++i, ++p)
        {
            outBuf[p] = context = work[i] ^ context;
        }
        return validBits;
    }

    public override string ToString()
    {
        return nameof(XorBinaryPacking);
    }
}