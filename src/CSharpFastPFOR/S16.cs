/**
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

/**
 * Version of Simple16 for NewPFD and OptPFD.
 * <p>
 * Adapted by D. Lemire from the Apache Lucene project.
 * </p>
 */

namespace Genbox.CSharpFastPFOR;

public class S16
{
    /**
     * Compress an integer array using Simple16
     *
     *
     * @param in
     *                array to compress
     * @param currentPos
     *                where to start reading
     * @param inlength
     *                how many integers to read
     * @param out   output array
     * @param tmpoutpos location in the output array
     * @return the number of 32-bit words written (in compressed form)
     */
    public static int compress(int[] @in, int currentPos, int inlength, int[] @out, int tmpoutpos)
    {
        int outpos = tmpoutpos;
        int finalin = currentPos + inlength;
        while (currentPos < finalin)
        {
            int inoffset = compressblock(@out, outpos++, @in,
                currentPos, inlength);
            if (inoffset == -1)
                throw new Exception("Too big a number");
            currentPos += inoffset;
            inlength -= inoffset;
        }
        return outpos - tmpoutpos;
    }

    /**
     * Estimate size of the compressed output.
     *
     * @param in
     *                array to compress
     * @param currentPos
     *                where to start reading
     * @param inlength
     *                how many integers to read
     * @return estimated size of the output (in 32-bit integers)
     */
    public static int estimatecompress(int[] @in, int currentPos, int inlength)
    {
        int finalin = currentPos + inlength;
        int counter = 0;
        while (currentPos < finalin)
        {
            int inoffset = fakecompressblock(@in, currentPos,
                inlength);
            if (inoffset == -1)
                throw new Exception("Too big a number");
            currentPos += inoffset;
            inlength -= inoffset;
            ++counter;
        }
        return counter;
    }

    /**
     * Compress an integer array using Simple16
     *
     * @param out
     *                the compressed output
     * @param outOffset
     *                the offset of the output in the number of integers
     * @param in
     *                the integer input array
     * @param inOffset
     *                the offset of the input in the number of integers
     * @param n
     *                the number of elements to be compressed
     * @return the size of the outputs in 32-bit integers
     *
     */
    public static int compressblock(int[] @out, int outOffset, int[] @in, int inOffset, int n)
    {
        int numIdx, j, num, bits;
        for (numIdx = 0; numIdx < S16_NUMSIZE; numIdx++)
        {
            @out[outOffset] = numIdx << S16_BITSSIZE;
            num = (S16_NUM[numIdx] < n) ? S16_NUM[numIdx] : n;

            for (j = 0, bits = 0; (j < num)
                                  && (@in[inOffset + j] < SHIFTED_S16_BITS[numIdx][j]);)
            {
                @out[outOffset] |= (@in[inOffset + j] << bits);
                bits += S16_BITS[numIdx][j];
                j++;
            }

            if (j == num)
            {
                return num;
            }
        }

        return -1;
    }

    private static int fakecompressblock(int[] @in, int inOffset, int n)
    {
        int numIdx, j, num;
        for (numIdx = 0; numIdx < S16_NUMSIZE; numIdx++)
        {
            num = (S16_NUM[numIdx] < n) ? S16_NUM[numIdx] : n;

            for (j = 0; (j < num)
                        && (@in[inOffset + j] < SHIFTED_S16_BITS[numIdx][j]);)
            {
                j++;
            }

            if (j == num)
            {
                return num;
            }
        }

        return -1;
    }

    /**
     * Decompress an integer array using Simple16
     *
     * @param out
     *                the decompressed output
     * @param outOffset
     *                the offset of the output in the number of integers
     * @param in
     *                the compressed input array
     * @param inOffset
     *                the offset of the input in the number of integers
     * @param n
     *                the number of elements to be compressed
     * @return the number of processed integers
     */
    public static int decompressblock(int[] @out, int outOffset, int[] @in, int inOffset, int n)
    {
        int j, bits;
        int numIdx = @in[inOffset] >>> S16_BITSSIZE;
        int num = S16_NUM[numIdx] < n ? S16_NUM[numIdx] : n;
        for (j = 0, bits = 0; j < num; j++)
        {
            @out[outOffset + j] = (int)(@in[inOffset] >>> bits & 0xffffffff >>> (32 - S16_BITS[numIdx][j]));
            bits += S16_BITS[numIdx][j];
        }
        return num;
    }

    /**
     * Uncompressed data from an input array into an output array
     *
     * @param in    input array (in compressed form)
     * @param tmpinpos  starting location in the compressed input array
     * @param inlength  how much data we wish the read (in 32-bit words)
     * @param out       output array (in decompressed form)
     * @param currentPos    current position in the output array
     * @param outlength     available data in the output array
     */
    public static void uncompress(int[] @in, int tmpinpos, int inlength, int[] @out, int currentPos, int outlength)
    {
        int pos = tmpinpos + inlength;
        while (tmpinpos < pos)
        {
            int howmany = decompressblock(@out, currentPos,
                @in, tmpinpos, outlength);
            outlength -= howmany;
            currentPos += howmany;
            tmpinpos += 1;
        }
    }

    private static int[][] shiftme(int[][] x)
    {
        int[][] answer = new int[x.Length][];
        for (int k = 0; k < x.Length; ++k)
        {
            answer[k] = new int[x[k].Length];
            for (int z = 0; z < answer[k].Length; ++z)
                answer[k][z] = 1 << x[k][z];
        }
        return answer;
    }

    private const int S16_NUMSIZE = 16;
    private const int S16_BITSSIZE = 28;
    // the possible number of bits used to represent one integer
    private static int[] S16_NUM = [28, 21, 21, 21, 14, 9, 8, 7, 6, 6, 5, 5, 4, 3, 2, 1];
    // the corresponding number of elements for each value of the number of
    // bits
    private static int[][] S16_BITS =
    [
        [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
        [2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
        [1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1],
        [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2],
        [2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2],
        [4, 3, 3, 3, 3, 3, 3, 3, 3], [3, 4, 4, 4, 4, 3, 3, 3],
        [4, 4, 4, 4, 4, 4, 4], [5, 5, 5, 5, 4, 4],
        [4, 4, 5, 5, 5, 5], [6, 6, 6, 5, 5], [5, 5, 6, 6, 6],
        [7, 7, 7, 7], [10, 9, 9], [14, 14], [28]
    ];
    private static int[][] SHIFTED_S16_BITS = shiftme(S16_BITS);

    public override string ToString()
    {
        return nameof(S16);
    }
}