#pragma warning disable S1118 // Preserve the constructible Java utility type.
namespace Genbox.CSharpFastPFOR.IntegerCompression.Port;

public class Integer
{
    public Integer(int v)
    {
        GC.KeepAlive(v);
    }

    public static int numberOfLeadingZeros(int x)
    {
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return (sizeof(int) * 8) - Ones(x);
    }

    public static int Ones(int x)
    {
        x -= (x >> 1) & 0x55555555;
        x = ((x >> 2) & 0x33333333) + (x & 0x33333333);
        x = ((x >> 4) + x) & 0x0f0f0f0f;
        x += x >> 8;
        x += x >> 16;
        return x & 0x0000003f;
    }
}