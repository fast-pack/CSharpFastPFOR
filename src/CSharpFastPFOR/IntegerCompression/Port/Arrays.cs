#pragma warning disable S1118 // Preserve the constructible Java utility type.
namespace Genbox.CSharpFastPFOR.IntegerCompression.Port;

public class Arrays
{
    private static void rangeCheck(int arrayLen, int fromIndex, int toIndex)
    {
        if (fromIndex > toIndex)
            throw new ArgumentOutOfRangeException("fromIndex(" + fromIndex + ") > toIndex(" + toIndex + ")");

        if (fromIndex < 0)
#if NETSTANDARD2_0
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
#else
            ArgumentOutOfRangeException.ThrowIfNegative(fromIndex);
#endif

        if (toIndex > arrayLen)
#if NETSTANDARD2_0
            throw new ArgumentOutOfRangeException(nameof(toIndex));
#else
            ArgumentOutOfRangeException.ThrowIfGreaterThan(toIndex, arrayLen);
#endif
    }

    public static void fill<T>(T[] array, int start, int end, T value)
    {
#if NETSTANDARD2_0
        if (array is null)
            throw new ArgumentNullException(nameof(array));
#else
        ArgumentNullException.ThrowIfNull(array);
#endif

        rangeCheck(array.Length, start, end);

        for (int i = start; i < end; i++)
            array[i] = value;
    }

    public static T[] copyOf<T>(T[] original, int newLength)
    {
        T[] copy = new T[newLength];
        Array.Copy(original, 0, copy, 0, Math.Min(original.Length, newLength));
        return copy;
    }

    public static T[] copyOfRange<T>(T[] original, int from, int to)
    {
        int newLength = to - from;
        if (newLength < 0)
            throw new ArgumentException(from + " > " + to);

        T[] copy = new T[newLength];
        Array.Copy(original, from, copy, 0, Math.Min(original.Length - from, newLength));
        return copy;
    }

    public static void fill<T>(T[] array, T value)
    {
        fill(array, 0, array.Length, value);
    }

    public static void sort(int[] array)
    {
        Array.Sort(array);
    }

    public static string toString(int[] ints) => string.Join(", ", ints);

    public static bool equals(int[] first, int[] second) => first.SequenceEqual(second);
}