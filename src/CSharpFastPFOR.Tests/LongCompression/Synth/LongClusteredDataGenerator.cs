namespace Genbox.CSharpFastPFOR.Tests.LongCompression.Synth;

internal class LongClusteredDataGenerator
{
    private readonly LongUniformDataGenerator _unidg = new LongUniformDataGenerator();

    private void fillUniform(long[] array, int offset, int length, long min, long max)
    {
        long[] v = _unidg.generateUniform(length, max - min);
        for (int k = 0; k < v.Length; ++k)
            array[k + offset] = min + v[k];
    }

    private void fillClustered(long[] array, int offset, int length, long min, long max)
    {
        long range = max - min;
        if (range == length || length <= 10)
        {
            fillUniform(array, offset, length, min, max);
            return;
        }

        long cut = (length / 2) + (range - length - 1 > 0 ? (long)_unidg.rand.NextDouble() * (range - length - 1) : 0);
        double p = _unidg.rand.NextDouble();
        if (p < 0.25)
        {
            fillUniform(array, offset, length / 2, min, min + cut);
            fillClustered(array, offset + (length / 2), length - (length / 2), min + cut, max);
        }
        else if (p < 0.5)
        {
            fillClustered(array, offset, length / 2, min, min + cut);
            fillUniform(array, offset + (length / 2), length - (length / 2), min + cut, max);
        }
        else
        {
            fillClustered(array, offset, length / 2, min, min + cut);
            fillClustered(array, offset + (length / 2), length - (length / 2), min + cut, max);
        }
    }

    public long[] generateClustered(int N, long Max)
    {
        long[] array = new long[N];
        fillClustered(array, 0, N, 0, Max);
        return array;
    }
}