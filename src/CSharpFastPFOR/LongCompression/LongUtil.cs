#pragma warning disable S1118, S1133 // Preserve the deprecated Java compatibility utility.
/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

namespace Genbox.CSharpFastPFOR.LongCompression;

/*
 * These are unofficial helpers related to long compression
 *
 * @author Benoit Lacelle
 */
[Obsolete("Use supported long compression codecs instead.")]
public class LongUtil
{
    /*
     * Compute the maximum of the integer logarithms (ceil(log(x+1)) of a range
     * of value
     *
     * @param i
     * source array
     * @param pos
     * starting position
     * @param length
     * number of integers to consider
     * @return integer logarithm
     */
    public static int maxbits(long[] i, int pos, int length)
    {
        long mask = 0;
        for (int k = pos; k < pos + length; ++k)
            mask |= i[k];
        return bits(mask);
    }

    /*
     * Compute the integer logarithms (ceil(log(x+1)) of a value
     *
     * @param i
     * source value
     * @return integer logarithm
     */
    public static int bits(long i) => 64 - NumberOfLeadingZeros(i);

    private static int NumberOfLeadingZeros(long value)
    {
        ulong x = (ulong)value;
        if (x == 0)
            return 64;

        int n = 0;
        for (ulong bit = 1UL << 63; (x & bit) == 0; bit >>= 1)
            ++n;
        return n;
    }

    protected internal static string longToBinaryWithLeading(long l) => Convert.ToString(l, 2).PadLeft(64, '0');
}