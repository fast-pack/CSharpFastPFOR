/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

/*
 * Delta+Zigzag Encoding.
 *
 * @author MURAOKA Taro http://github.com/koron
 */
using System.Diagnostics.CodeAnalysis;

namespace Genbox.CSharpFastPFOR.IntegerCompression;

[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public sealed class DeltaZigzagEncoding
{
    internal class Context
    {
        protected int ContextValue;

        protected Context(int contextValue)
        {
            ContextValue = contextValue;
        }

        internal void setContextValue(int contextValue)
        {
            ContextValue = contextValue;
        }

        internal int getContextValue() => ContextValue;
    }

    internal class Encoder(int contextValue) : Context(contextValue)
    {
        internal int encodeInt(int value)
        {
            int n = value - ContextValue;
            ContextValue = value;
            return (n << 1) ^ (n >> 31);
        }

        private int[] encodeArray(int[] src, int srcoff, int length, int[] dst, int dstoff)
        {
            for (int i = 0; i < length; ++i)
                dst[dstoff + i] = encodeInt(src[srcoff + i]);
            return dst;
        }

        internal int[] encodeArray(int[] src, int srcoff, int length, int[] dst) => encodeArray(src, srcoff, length, dst, 0);

        internal int[] encodeArray(int[] src, int offset, int length) => encodeArray(src, offset, length, new int[length], 0);

        internal int[] encodeArray(int[] src) => encodeArray(src, 0, src.Length, new int[src.Length], 0);
    }

    internal class Decoder(int contextValue) : Context(contextValue)
    {
        internal int decodeInt(int value)
        {
            int n = (value >>> 1) ^ ((value << 31) >> 31);
            n += ContextValue;
            ContextValue = n;
            return n;
        }

        internal int[] decodeArray(int[] src,
            int srcoff,
            int length,
            int[] dst,
            int dstoff)
        {
            for (int i = 0; i < length; ++i)
                dst[dstoff + i] = decodeInt(src[srcoff + i]);
            return dst;
        }

        internal int[] decodeArray(int[] src, int offset, int length) => decodeArray(src, offset, length, new int[length], 0);

        internal int[] decodeArray(int[] src) => decodeArray(src, 0, src.Length);
    }
}