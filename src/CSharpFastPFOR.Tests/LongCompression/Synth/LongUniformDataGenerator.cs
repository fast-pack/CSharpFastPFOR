/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

using System.Diagnostics;
using Genbox.CSharpFastPFOR.IntegerCompression.Synth;
using Genbox.CSharpFastPFOR.Tests.LongCompression.Port;

namespace Genbox.CSharpFastPFOR.Tests.LongCompression.Synth;

/*
 * This class will generate "uniform" lists of random longs.
 *
 * @author Benoit Lacelle
 * @see UniformDataGenerator
 */
internal class LongUniformDataGenerator
{
    internal readonly JavaRandom rand = new JavaRandom();

    /*
     * generates randomly N distinct longs from 0 to Max.
     */
    private long[] generateUniformHash(int N, long Max)
    {
        if (N > Max)
            throw new InvalidOperationException("not possible");
        long[] ans = new long[N];
        HashSet<long> s = new HashSet<long>();
        while (s.Count < N)
            s.Add((long)(rand.NextDouble() * Max));
        HashSet<long>.Enumerator i = s.GetEnumerator();
        for (int k = 0; k < N; ++k)
        {
            i.MoveNext();
            ans[k] = i.Current;
        }
        Array.Sort(ans);
        return ans;
    }

    /*
     * output all longs from the range [0,Max) that are not in the array
     */
    private static long[] negate(long[] x, long Max)
    {
        int newLength = saturatedCast(Max - x.Length);
        long[] ans = new long[newLength];
        int i = 0;
        int c = 0;
        foreach (long v in x)
        {
            for (; i < v; ++i)
                ans[c++] = i;
            ++i;
        }
        while (c < ans.Length)
            ans[c++] = i++;
        return ans;
    }

    private static int saturatedCast(long toInt)
    {
        if (toInt > int.MaxValue)
            return int.MaxValue;
        return (int)toInt;
    }

    /*
     * generates randomly N distinct longs from 0 to Max.
     *
     * @param N
     * number of longs to generate
     * @param Max
     * bound on the value of longs
     * @return an array containing randomly selected longs
     */
    public long[] generateUniform(int N, long Max)
    {
        Debug.Assert(N >= 0);
        Debug.Assert(Max >= 0);
        if (N * 2 > Max)
            return negate(generateUniform(saturatedCast(Max - N), Max), Max);
        if (2048 * N > Max)
            return generateUniformBitmap(N, Max);
        return generateUniformHash(N, Max);
    }

    /*
     * generates randomly N distinct longs from 0 to Max.
     */
    private long[] generateUniformBitmap(int N, long Max)
    {
        if (N > Max)
            throw new InvalidOperationException("not possible");
        Roaring64Bitmap bs = new Roaring64Bitmap();
        int cardinality = 0;
        while (cardinality < N)
        {
            long v = (long)(rand.NextDouble() * Max);
            if (!bs.contains(v))
            {
                bs.add(v);
                cardinality++;
            }
        }
        return bs.toArray();
    }
}