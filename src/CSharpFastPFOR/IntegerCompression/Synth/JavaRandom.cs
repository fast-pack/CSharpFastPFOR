namespace Genbox.CSharpFastPFOR.IntegerCompression.Synth;

public sealed class JavaRandom(int seed)
{
    private const long Multiplier = 0x5DEECE66DL;
    private const long Addend = 0xBL;
    private const long Mask = (1L << 48) - 1;
    private long _seed = (seed ^ Multiplier) & Mask;

    public JavaRandom()
        : this(Environment.TickCount) {}

    public int Next(int bound)
    {
        if (bound <= 0)
            throw new ArgumentException("bound must be positive");

        if ((bound & -bound) == bound)
            return (int)((bound * (long)NextBits(31)) >> 31);

        int bits;
        int val;
        do
        {
            bits = NextBits(31);
            val = bits % bound;
        } while ((bits - val) + (bound - 1) < 0);

        return val;
    }

    public double NextDouble() => (((long)NextBits(26) << 27) + NextBits(27)) / (double)(1L << 53);

    private int NextBits(int bits)
    {
        _seed = ((_seed * Multiplier) + Addend) & Mask;
        return (int)(_seed >>> (48 - bits));
    }
}