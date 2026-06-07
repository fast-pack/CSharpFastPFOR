using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using static Genbox.CSharpFastPFOR.IntegerCompression.Vector.VectorBitPackerKernels;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Vector;

public sealed class VectorBinaryPacking : IntegerCODEC, SkippableIntegerCODEC
{
    public const int BLOCK_SIZE = 256;
    private const int MAX_BIT_WIDTH = 32;
    private const int WORDS_PER_BLOCK_BIT = BLOCK_SIZE / 32;
    private const int GROUP_SIZE_IN_BLOCKS = 4;
    private static readonly VectorSpecies<int> MAXBITS_SPECIES = IntVector.SPECIES_PREFERRED;
    private readonly VectorBitPackerKernels kernel;

    public VectorBinaryPacking() : this(LaneWidth.PREFERRED) {}

    public VectorBinaryPacking(LaneWidth laneWidth) => kernel = laneWidth.Kernel;

    public void compress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        inlength = Util.greatestMultiple(inlength, BLOCK_SIZE);
        if (inlength == 0)
            return;
        @out[outpos.get()] = inlength;
        outpos.increment();
        headlessCompress(@in, inpos, inlength, @out, outpos);
    }

    public void headlessCompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        inlength = Util.greatestMultiple(inlength, BLOCK_SIZE);
        int tmpoutpos = outpos.get();
        int s = inpos.get();
        for (; s + (BLOCK_SIZE * 4) - 1 < inpos.get() + inlength; s += BLOCK_SIZE * 4)
        {
            int mbits1 = maxbits(@in, s);
            int mbits2 = maxbits(@in, s + BLOCK_SIZE);
            int mbits3 = maxbits(@in, s + (2 * BLOCK_SIZE));
            int mbits4 = maxbits(@in, s + (3 * BLOCK_SIZE));
            @out[tmpoutpos++] = (mbits1 << 24) | (mbits2 << 16) | (mbits3 << 8) | mbits4;
            kernel.fastpackNoMask(@in, s, @out, tmpoutpos, mbits1);
            tmpoutpos += WORDS_PER_BLOCK_BIT * mbits1;
            kernel.fastpackNoMask(@in, s + BLOCK_SIZE, @out, tmpoutpos, mbits2);
            tmpoutpos += WORDS_PER_BLOCK_BIT * mbits2;
            kernel.fastpackNoMask(@in, s + (2 * BLOCK_SIZE), @out, tmpoutpos, mbits3);
            tmpoutpos += WORDS_PER_BLOCK_BIT * mbits3;
            kernel.fastpackNoMask(@in, s + (3 * BLOCK_SIZE), @out, tmpoutpos, mbits4);
            tmpoutpos += WORDS_PER_BLOCK_BIT * mbits4;
        }
        for (; s < inpos.get() + inlength; s += BLOCK_SIZE)
        {
            int mbits = maxbits(@in, s);
            @out[tmpoutpos++] = mbits;
            kernel.fastpackNoMask(@in, s, @out, tmpoutpos, mbits);
            tmpoutpos += WORDS_PER_BLOCK_BIT * mbits;
        }
        inpos.add(inlength);
        outpos.set(tmpoutpos);
    }

    public void uncompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos)
    {
        if (inlength == 0)
            return;
        int outlength = @in[inpos.get()];
        inpos.increment();
        headlessUncompress(@in, inpos, inlength, @out, outpos, outlength);
    }

    public void headlessUncompress(int[] @in, IntWrapper inpos, int inlength, int[] @out, IntWrapper outpos, int num)
    {
        int outlength = Util.greatestMultiple(num, BLOCK_SIZE);
        int tmpinpos = inpos.get();
        int s = outpos.get();
        for (; s + (BLOCK_SIZE * 4) - 1 < outpos.get() + outlength; s += BLOCK_SIZE * 4)
        {
            int mbits1 = @in[tmpinpos] >>> 24;
            int mbits2 = (@in[tmpinpos] >>> 16) & 0xFF;
            int mbits3 = (@in[tmpinpos] >>> 8) & 0xFF;
            int mbits4 = @in[tmpinpos] & 0xFF;
            tmpinpos++;
            kernel.fastunpack(@in, tmpinpos, @out, s, mbits1);
            tmpinpos += WORDS_PER_BLOCK_BIT * mbits1;
            kernel.fastunpack(@in, tmpinpos, @out, s + BLOCK_SIZE, mbits2);
            tmpinpos += WORDS_PER_BLOCK_BIT * mbits2;
            kernel.fastunpack(@in, tmpinpos, @out, s + (2 * BLOCK_SIZE), mbits3);
            tmpinpos += WORDS_PER_BLOCK_BIT * mbits3;
            kernel.fastunpack(@in, tmpinpos, @out, s + (3 * BLOCK_SIZE), mbits4);
            tmpinpos += WORDS_PER_BLOCK_BIT * mbits4;
        }
        for (; s < outpos.get() + outlength; s += BLOCK_SIZE)
        {
            int mbits = @in[tmpinpos++];
            kernel.fastunpack(@in, tmpinpos, @out, s, mbits);
            tmpinpos += WORDS_PER_BLOCK_BIT * mbits;
        }
        outpos.add(outlength);
        inpos.set(tmpinpos);
    }

    public int maxHeadlessCompressedLength(IntWrapper compressedPositions, int inlength)
    {
        int blockCount = inlength / BLOCK_SIZE;
        int headersSizeInInts = (blockCount / GROUP_SIZE_IN_BLOCKS) + (blockCount % GROUP_SIZE_IN_BLOCKS);
        compressedPositions.add(blockCount * BLOCK_SIZE);
        return headersSizeInInts + (blockCount * MAX_BIT_WIDTH * WORDS_PER_BLOCK_BIT);
    }

    private static int maxbits(int[] @in, int pos)
    {
        IntVector accumulator = IntVector.broadcast(MAXBITS_SPECIES, 0);
        for (int offset = 0; offset < BLOCK_SIZE; offset += MAXBITS_SPECIES.Length)
            accumulator = accumulator.or(IntVector.fromArray(MAXBITS_SPECIES, @in, pos + offset));
        return 32 - Integer.numberOfLeadingZeros(accumulator.reduceLanes(VectorOperators.OR));
    }

    public override string ToString() => $"{GetType().Name}({kernel.GetType().Name})";
}