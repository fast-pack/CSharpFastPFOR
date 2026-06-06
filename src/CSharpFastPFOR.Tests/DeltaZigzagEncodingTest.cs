/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

using Genbox.CSharpFastPFOR.Tests.Port;

namespace Genbox.CSharpFastPFOR.Tests;

public class DeltaZigzagEncodingTest
{
    private static int zigzagEncode(DeltaZigzagEncoding.Encoder e, int value)
    {
        e.setContextValue(0);
        return e.encodeInt(value);
    }

    private static int zigzagDecode(DeltaZigzagEncoding.Decoder d, int value)
    {
        d.setContextValue(0);
        return d.decodeInt(value);
    }

    private static void checkEncode(
        DeltaZigzagEncoding.Encoder e,
        int[] data,
        int[] expected)
    {
        Assert2.assertArrayEquals(expected, e.encodeArray(data));
        Assert2.assertEquals(data[data.Length - 1], e.getContextValue());
    }

    private static void checkDecode(
        DeltaZigzagEncoding.Decoder d,
        int[] data,
        int[] expected)
    {
        int[] r = d.decodeArray(data);
        Assert2.assertArrayEquals(expected, r);
        Assert2.assertEquals(r[r.Length - 1], d.getContextValue());
    }

    [Fact]
    public void checkZigzagEncode()
    {
        DeltaZigzagEncoding.Encoder e = new DeltaZigzagEncoding.Encoder(0);
        Assert2.assertEquals(0, zigzagEncode(e, 0));
        Assert2.assertEquals(2, zigzagEncode(e, 1));
        Assert2.assertEquals(4, zigzagEncode(e, 2));
        Assert2.assertEquals(6, zigzagEncode(e, 3));
        Assert2.assertEquals(1, zigzagEncode(e, -1));
        Assert2.assertEquals(3, zigzagEncode(e, -2));
        Assert2.assertEquals(5, zigzagEncode(e, -3));
    }

    [Fact]
    public void checkZigzagDecoder()
    {
        DeltaZigzagEncoding.Decoder d = new DeltaZigzagEncoding.Decoder(0);
        Assert2.assertEquals(0, zigzagDecode(d, 0));
        Assert2.assertEquals(-1, zigzagDecode(d, 1));
        Assert2.assertEquals(1, zigzagDecode(d, 2));
        Assert2.assertEquals(-2, zigzagDecode(d, 3));
        Assert2.assertEquals(2, zigzagDecode(d, 4));
        Assert2.assertEquals(-3, zigzagDecode(d, 5));
    }

    [Fact]
    public void checkEncodeSimple()
    {
        DeltaZigzagEncoding.Encoder e = new DeltaZigzagEncoding.Encoder(0);
        checkEncode(e,
            [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
            [0, 2, 2, 2, 2, 2, 2, 2, 2, 2]);
    }

    [Fact]
    public void checkDecodeSimple()
    {
        DeltaZigzagEncoding.Decoder d = new DeltaZigzagEncoding.Decoder(0);
        checkDecode(d,
            [0, 2, 2, 2, 2, 2, 2, 2, 2, 2],
            [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
    }

    private class SpotChecker
    {

        private static readonly DeltaZigzagEncoding.Encoder encoder = new DeltaZigzagEncoding.Encoder(0);
        private static readonly DeltaZigzagEncoding.Decoder decoder = new DeltaZigzagEncoding.Decoder(0);

        public void check(int value)
        {
            SpotChecker.encoder.setContextValue(0);
            SpotChecker.decoder.setContextValue(0);
            int value2 = SpotChecker.decoder.decodeInt(SpotChecker.encoder.encodeInt(value));
            Assert2.assertEquals(value, value2);
        }
    }

    [Fact]
    public void checkSpots()
    {
        SpotChecker c = new SpotChecker();
        c.check(0);
        c.check(1);
        c.check(1375228800);
        c.check(1 << 30);
        c.check(1 << 31);
    }
}