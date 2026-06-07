namespace Genbox.CSharpFastPFOR.Tests.LongCompression.Port;

internal sealed class Roaring64Bitmap
{
    private readonly SortedSet<long> values = new SortedSet<long>();

    public bool contains(long value) => values.Contains(value);

    public void add(long value)
    {
        values.Add(value);
    }

    public long[] toArray()
    {
        long[] result = new long[values.Count];
        values.CopyTo(result);
        return result;
    }
}