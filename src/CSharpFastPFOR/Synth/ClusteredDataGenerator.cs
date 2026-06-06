/**
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 * (c) Daniel Lemire, http://lemire.me/en/
 */

/**
 * This class will generate lists of random integers based on the clustered
 * model:
 *
 * Reference: Vo Ngoc Anh and Alistair Moffat. 2010. Index compression using
 * 64-bit words. Softw. Pract. Exper.40, 2 (February 2010), 131-147.
 *
 * @author Daniel Lemire
 */

namespace Genbox.CSharpFastPFOR.Synth;

public class ClusteredDataGenerator
{
    private readonly UniformDataGenerator unidg = new UniformDataGenerator();

    /**
     * Creating random array generator.
     */
    public ClusteredDataGenerator()
    {
    }

    private void fillUniform(int[] array, int offset, int length, int Min, int Max)
    {
        int[] v = this.unidg.generateUniform(length, Max - Min);
        for (int k = 0; k < v.Length; ++k)
            array[k + offset] = Min + v[k];
    }

    private void fillClustered(int[] array, int offset, int length, int Min, int Max)
    {
        int range = Max - Min;
        if ((range == length) || (length <= 10))
        {
            fillUniform(array, offset, length, Min, Max);
            return;
        }
        int cut = (length / 2) + ((range - length - 1 > 0) ? this.unidg.rand.Next(range - length - 1) : 0);
        double p = this.unidg.rand.NextDouble();
        if (p < 0.25)
        {
            fillUniform(array, offset, length / 2, Min, Min + cut);
            fillClustered(array, offset + (length / 2), length - (length / 2), Min + cut, Max);
        }
        else if (p < 0.5)
        {
            fillClustered(array, offset, length / 2, Min, Min + cut);
            fillUniform(array, offset + (length / 2), length - (length / 2), Min + cut, Max);
        }
        else
        {
            fillClustered(array, offset, length / 2, Min, Min + cut);
            fillClustered(array, offset + (length / 2), length - (length / 2), Min + cut, Max);
        }
    }

    /**
     * generates randomly N distinct integers from 0 to Max.
     *
     * @param N
     *                number of integers to generate
     * @param Max
     *                maximal value of the integers
     * @return array containing the integers
     */
    public int[] generateClustered(int N, int Max)
    {
        int[] array = new int[N];
        fillClustered(array, 0, N, 0, Max);
        return array;
    }
}