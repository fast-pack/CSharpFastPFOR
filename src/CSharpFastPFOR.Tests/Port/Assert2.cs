namespace Genbox.CSharpFastPFOR.Tests.Port;

public static class Assert2
{
    public static void assertArrayEquals(int[] ints, int[] ints1)
    {
        Assert.True(ints.SequenceEqual(ints1));
    }

    public static void assertEquals(int first, int second)
    {
        Assert.Equal(first,second);
    }

    public static void assertTrue(bool b)
    {
        Assert.True(b);
    }
}