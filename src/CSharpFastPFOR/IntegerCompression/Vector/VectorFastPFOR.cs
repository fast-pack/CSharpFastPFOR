/*
 * This code is released under the Apache License Version 2.0.
 * (c) Daniel Lemire, http://lemire.me/en/
 * (c) Intel Corp. (for Vector implementation)
 */
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using static Genbox.CSharpFastPFOR.IntegerCompression.Vector.VectorBitPackerKernels;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Vector;

public class VectorFastPFOR : IntegerCODEC, SkippableIntegerCODEC
{
    private const int OVERHEAD_OF_EACH_EXCEPT = 8;
    private const int OVERHEAD_OF_EACH_PAGE_IN_INTS = 36;
    private const int OVERHEAD_OF_EACH_BLOCK_IN_INTS = 1;
    public const int DEFAULT_PAGE_SIZE = 64 << 10;
    public const int BLOCK_SIZE = 256;
    private const int INTS_PER_BLOCK = BLOCK_SIZE >>> 5;

    // The page header stores the metadata offset and the packing lane width.
    private const int WIDTH_SHIFT = 30;
    private const int WHEREMETA_MASK = (1 << WIDTH_SHIFT) - 1;

    private readonly int pageSize;
    private readonly LaneWidth encodeWidth;
    private readonly VectorBitPackerKernels encoder;
    private readonly int[][] dataTobePacked = new int[33][];
    private readonly int[] dataPointers = new int[33];
    private readonly int[] freqs = new int[33];
    private readonly byte[] bem;

    public VectorFastPFOR(int pagesize, LaneWidth encodeWidth)
    {
        if (pagesize >= 1 << WIDTH_SHIFT)
            throw new ArgumentException($"page size must be smaller than {1 << WIDTH_SHIFT}");

        pageSize = pagesize;
        this.encodeWidth = encodeWidth;
        encoder = encodeWidth.Kernel;
        bem = new byte[((3 * pageSize) / BLOCK_SIZE) + pageSize];
        for (int k = 1; k < dataTobePacked.Length; ++k)
            dataTobePacked[k] = new int[(pageSize / 32) * 4];
    }

    public VectorFastPFOR() : this(DEFAULT_PAGE_SIZE, LaneWidth.PREFERRED) {}

    public void compress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        inlength -= inlength % BLOCK_SIZE;
        if (inlength == 0)
            return;
        @out[outpos.get()] = inlength;
        outpos.increment();
        headlessCompress(@in, inpos, inlength, @out, outpos);
    }

    public void uncompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        if (inlength == 0)
            return;
        int outlength = @in[inpos.get()];
        inpos.increment();
        headlessUncompress(@in, inpos, inlength, @out, outpos, outlength);
    }

    public void headlessCompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        inlength -= inlength % BLOCK_SIZE;
        int finalinpos = inpos.get() + inlength;
        while (inpos.get() != finalinpos)
            encodePage(@in, inpos, Math.Min(pageSize, finalinpos - inpos.get()), @out, outpos);
    }

    public void headlessUncompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos, int num)
    {
        num -= num % BLOCK_SIZE;
        int finalout = outpos.get() + num;
        while (outpos.get() != finalout)
            decodePage(@in, inpos, @out, outpos, Math.Min(pageSize, finalout - outpos.get()));
    }

    public int maxHeadlessCompressedLength(IntWrapper compressedPositions, int inlength)
    {
        inlength -= inlength % BLOCK_SIZE;
        int pageCount = (inlength + pageSize - 1) / pageSize;
        int blockCount = inlength / BLOCK_SIZE;
        return (OVERHEAD_OF_EACH_PAGE_IN_INTS * pageCount) +
               ((OVERHEAD_OF_EACH_BLOCK_IN_INTS + BLOCK_SIZE) * blockCount) + 24;
    }

    private void getBestBitSize(int[] @in, int pos, int index)
    {
        Array.Clear(freqs, 0, freqs.Length);
        for (int i = pos, limit = pos + BLOCK_SIZE; i < limit; i++)
            freqs[32 - Integer.numberOfLeadingZeros(@in[i])]++;
        bem[index] = 32;
        while (freqs[bem[index]] == 0)
            bem[index]--;
        bem[index + 2] = bem[index];
        int maxb = bem[index + 2];
        int bestcost = bem[index] * BLOCK_SIZE;
        int cexcept = 0;
        bem[index + 1] = 0;
        for (int b = bem[index] - 1; b >= 0; --b)
        {
            cexcept += freqs[b + 1];
            if (cexcept == BLOCK_SIZE)
                break;
            int thiscost = (cexcept * OVERHEAD_OF_EACH_EXCEPT) + (cexcept * (maxb - b)) + (b * BLOCK_SIZE) + 8;
            if (maxb - b == 1)
                thiscost -= cexcept;
            if (thiscost < bestcost)
            {
                bestcost = thiscost;
                bem[index] = (byte)b;
                bem[index + 1] = (byte)cexcept;
            }
        }
    }

    private void encodePage(int[] @in, IntWrapper inpos, int thissize, int[] @out, IntWrapper outpos)
    {
        int headerpos = outpos.get();
        outpos.increment();
        int tmpoutpos = outpos.get();
        Array.Clear(dataPointers, 0, dataPointers.Length);
        Array.Clear(bem, 0, bem.Length);

        int tmpinpos = inpos.get();
        int finalinpos = tmpinpos + thissize - BLOCK_SIZE;
        int bindex = 0;
        for (; tmpinpos <= finalinpos; tmpinpos += BLOCK_SIZE)
        {
            getBestBitSize(@in, tmpinpos, bindex);
            int tmpexcept = bem[bindex + 1] & 0xFF;
            int tmpbestb = bem[bindex];
            if (tmpexcept > 0)
            {
                int index = bem[bindex + 2] - tmpbestb;
                if (dataPointers[index] + tmpexcept >= dataTobePacked[index].Length)
                {
                    int newsize = 2 * (dataPointers[index] + tmpexcept);
                    Array.Resize(ref dataTobePacked[index], Util.greatestMultiple(newsize + BLOCK_SIZE - 1, BLOCK_SIZE));
                }
                bindex += 3;
                for (int k = 0; k < BLOCK_SIZE; ++k)
                    if (@in[k + tmpinpos] >>> tmpbestb != 0)
                    {
                        bem[bindex++] = (byte)k;
                        dataTobePacked[index][dataPointers[index]++] = @in[k + tmpinpos] >>> tmpbestb;
                    }
            }
            else
                bindex += 2;
            encoder.fastpack(@in, tmpinpos, @out, tmpoutpos, tmpbestb);
            tmpoutpos += INTS_PER_BLOCK * tmpbestb;
        }
        inpos.set(tmpinpos);
        @out[headerpos] = (tmpoutpos - headerpos) | (encodeWidth.Code << WIDTH_SHIFT);

        int bytesize = bindex;
        @out[tmpoutpos++] = bytesize;
        bytesize = bytesize % 4 == 0 ? bytesize : ((bytesize / 4) * 4) + 4;
        for (int i = 0; i <= bytesize - 4; i += 4)
            @out[tmpoutpos++] = (bem[i] & 0xFF) | ((bem[i + 1] & 0xFF) << 8) | ((bem[i + 2] & 0xFF) << 16) | ((bem[i + 3] & 0xFF) << 24);

        int bitmap = 0;
        for (int k = 2; k <= 32; ++k)
            if (dataPointers[k] != 0)
                bitmap |= 1 << (k - 1);
        @out[tmpoutpos++] = bitmap;
        for (int k = 2; k <= 32; ++k)
            if (dataPointers[k] != 0)
            {
                @out[tmpoutpos++] = dataPointers[k];
                int j = 0;
                int n = (dataPointers[k] / BLOCK_SIZE) * BLOCK_SIZE;
                for (; j < n; j += BLOCK_SIZE)
                {
                    encoder.fastpackNoMask(dataTobePacked[k], j, @out, tmpoutpos, k);
                    tmpoutpos += INTS_PER_BLOCK * k;
                }
                int r = dataPointers[k] % BLOCK_SIZE;
                if (r != 0)
                {
                    tmpoutpos = slowpack(dataTobePacked[k], j, r, @out, tmpoutpos, k);
                    tmpoutpos++;
                }
            }
        outpos.set(tmpoutpos);
    }

    private void loadMetaData(int[] @in, int inexcept, int bytesize)
    {
        int len = (bytesize + 3) / 4;
        int lc = 0;
        for (int i = 0; i < len; i++)
        {
            bem[lc++] = (byte)@in[inexcept + i];
            bem[lc++] = (byte)(@in[inexcept + i] >>> 8);
            bem[lc++] = (byte)(@in[inexcept + i] >>> 16);
            bem[lc++] = (byte)(@in[inexcept + i] >>> 24);
        }
    }

    internal static void checkDecodable(LaneWidth streamWidth, LaneWidth hostWidth)
    {
        if (streamWidth.Bits > hostWidth.Bits)
            throw new InvalidOperationException($"VectorFastPFOR stream was packed for {streamWidth.Bits}-bit vector lanes, but this machine runs at most {hostWidth.Bits}-bit lanes natively. Re-encode with lanes <= {hostWidth.Bits} bits.");
    }

    private void decodePage(int[] @in, IntWrapper inpos, int[] @out, IntWrapper outpos, int thissize)
    {
        int initpos = inpos.get();
        int header = @in[inpos.get()];
        int wheremeta = header & WHEREMETA_MASK;
        LaneWidth streamWidth = LaneWidth.fromCode((int)((uint)header >> WIDTH_SHIFT));
        checkDecodable(streamWidth, LaneWidth.PREFERRED);
        VectorBitPackerKernels decoder = streamWidth.Kernel;
        inpos.increment();
        int inexcept = initpos + wheremeta;
        int bytesize = @in[inexcept++];
        loadMetaData(@in, inexcept, bytesize);
        inexcept += (bytesize + 3) / 4;
        int bitmap = @in[inexcept++];
        for (int k = 2; k <= 32; ++k)
            if ((bitmap & (1 << (k - 1))) != 0)
            {
                int size = @in[inexcept++];
                int roundedup = Util.greatestMultiple(size + BLOCK_SIZE - 1, BLOCK_SIZE);
                if (dataTobePacked[k].Length < roundedup)
                    dataTobePacked[k] = new int[roundedup];
                if (inexcept + ((roundedup / 32) * k) <= @in.Length)
                {
                    int j = 0;
                    int len = (size / BLOCK_SIZE) * BLOCK_SIZE;
                    for (; j < len; j += BLOCK_SIZE)
                    {
                        decoder.fastunpack(@in, inexcept, dataTobePacked[k], j, k);
                        inexcept += INTS_PER_BLOCK * k;
                    }
                    inexcept = slowunpack(@in, inexcept, dataTobePacked[k], j, size % BLOCK_SIZE, k);
                }
                else
                {
                    int bufferLength = Util.greatestMultiple(((roundedup / 32) * k) + BLOCK_SIZE - 1, BLOCK_SIZE);
                    int[] buffer = new int[bufferLength];
                    int initialExceptionPosition = inexcept;
                    Array.Copy(@in, inexcept, buffer, 0, @in.Length - inexcept);
                    int j = 0;
                    int len = (size / BLOCK_SIZE) * BLOCK_SIZE;
                    for (; j < len; j += BLOCK_SIZE)
                    {
                        decoder.fastunpack(buffer, inexcept - initialExceptionPosition, dataTobePacked[k], j, k);
                        inexcept += INTS_PER_BLOCK * k;
                    }
                    inexcept = slowunpack(@in, inexcept, dataTobePacked[k], j, size % BLOCK_SIZE, k);
                }
            }

        Array.Clear(dataPointers, 0, dataPointers.Length);
        int tmpoutpos = outpos.get();
        int tmpinpos = inpos.get();
        int idx = 0;
        for (int run = 0, runEnd = thissize / BLOCK_SIZE; run < runEnd; ++run, tmpoutpos += BLOCK_SIZE)
        {
            int b = bem[idx];
            int cexcept = bem[idx + 1] & 0xFF;
            decoder.fastunpack(@in, tmpinpos, @out, tmpoutpos, b);
            tmpinpos += INTS_PER_BLOCK * b;
            if (cexcept > 0)
            {
                int index = bem[idx + 2] - b;
                idx += 3;
                for (int k = 0; k < cexcept; ++k)
                {
                    int pos = bem[idx++] & 0xFF;
                    int exceptvalue = index == 1 ? 1 : dataTobePacked[index][dataPointers[index]++];
                    @out[pos + tmpoutpos] |= exceptvalue << b;
                }
            }
            else
                idx += 2;
        }
        outpos.set(tmpoutpos);
        inpos.set(inexcept);
    }

    // Packs scalar exception remainders after clearing the reused destination words.
    private static int slowpack(int[] @in, int inpos, int inlen, int[] @out, int outpos, int b)
    {
        if (inlen == 0)
            return outpos;
        if (b == 32)
        {
            Array.Copy(@in, inpos, @out, outpos, inlen);
            return outpos + inlen;
        }
        int mask = (1 << b) - 1;
        Array.Clear(@out, outpos, ((inlen * b) + 31) / 32);
        int c = 0, r = 0;
        for (int i = 0; i < inlen; i++)
        {
            int val = @in[inpos + i] & mask;
            @out[outpos] |= val << (c + r);
            c += b;
            int l = (32 - r) % b;
            if (c + r >= 32)
            {
                if (i < inlen - 1 || l != 0)
                    outpos++;
                r = l == 0 ? 0 : b - l;
                if (l != 0)
                    @out[outpos] = val >> (b - r);
                c = 0;
            }
        }
        return outpos;
    }

    private static int slowunpack(int[] @in, int inpos, int[] @out, int outpos, int outlen, int b)
    {
        if (outlen == 0)
            return inpos;
        if (b == 32)
        {
            Array.Copy(@in, inpos, @out, outpos, outlen);
            return inpos + outlen;
        }
        int mask = (1 << b) - 1;
        int limit = outpos + outlen;
        int r = 0, val = 0, i = 0;
        for (; outpos < limit; i++)
        {
            if (r > 0)
                @out[outpos++] = (val >>> (32 - (b - r))) | ((@in[inpos + i] << (b - r)) & mask);
            val = @in[inpos + i];
            int j = 0;
            int l = 32 - r;
            int ll = l % b == 0 ? l : l - b;
            while (j < ll && outpos < limit)
            {
                @out[outpos++] = (val >> (j + r)) & mask;
                j += b;
            }
            r = l % b == 0 ? 0 : b - (l % b);
        }
        return inpos + i;
    }

    public override string ToString() => GetType().Name;
}