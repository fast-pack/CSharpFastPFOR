/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

/*
 * This class will generate "uniform" lists of random integers.
 *
 * @author Daniel Lemire
 */

using Genbox.CSharpFastPFOR.IntegerCompression.Port;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Synth;

public class UniformDataGenerator
{
    internal readonly JavaRandom rand;

    /*
     * construct generator of random arrays.
     */
    public UniformDataGenerator()
    {
        rand = new JavaRandom();
    }

    /*
     * @param seed
     * random seed
     */
    public UniformDataGenerator(int seed)
    {
        rand = new JavaRandom(seed);
    }

    /*
     * generates randomly N distinct integers from 0 to Max.
     */
    private int[] generateUniformHash(int N, int Max)
    {
        if (N > Max)
            throw new InvalidOperationException("not possible");

        int[] ans = new int[N];
        HashSet<int> s = new HashSet<int>();
        while (s.Count < N)
            s.Add(rand.Next(Max));

        int k = 0;
        foreach (int value in s)
            ans[k++] = value;

        Arrays.sort(ans);
        return ans;
    }

    /*
     * output all integers from the range [0,Max) that are not in the array
     */
    private static int[] negate(int[] x, int Max)
    {
        int[] ans = new int[Max - x.Length];
        int i = 0;
        int c = 0;
        foreach (int v in x)
        {
            for (; i < v; ++i)
                ans[c++] = i;
            ++i;
        }
        while (c < ans.Length)
            ans[c++] = i++;
        return ans;
    }

    /*
     * generates randomly N distinct integers from 0 to Max.
     *
     * @param N
     * number of integers to generate
     * @param Max
     * bound on the value of integers
     * @return an array containing randomly selected integers
     */
    public int[] generateUniform(int N, int Max)
    {
        if (N * 2 > Max)
            return negate(generateUniform(Max - N, Max), Max);
        if (2048 * N > Max)
            return generateUniformBitmap(N, Max);
        return generateUniformHash(N, Max);
    }

    /*
     * generates randomly N distinct integers from 0 to Max.
     */
    private int[] generateUniformBitmap(int N, int Max)
    {
        if (N > Max)
            throw new InvalidOperationException("not possible");
        int[] ans = new int[N];
        BitSet bs = new BitSet(Max);
        int cardinality = 0;
        while (cardinality < N)
        {
            int v = rand.Next(Max);
            if (!bs.get(v))
            {
                bs.set(v);
                cardinality++;
            }
        }
        int pos = 0;
        for (int i = bs.nextSetBit(0); i >= 0; i = bs.nextSetBit(i + 1))
            ans[pos++] = i;
        return ans;
    }
}