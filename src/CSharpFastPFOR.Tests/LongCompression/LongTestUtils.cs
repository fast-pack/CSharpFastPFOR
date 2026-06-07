using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.LongCompression;

namespace Genbox.CSharpFastPFOR.Tests.LongCompression;

internal static class LongTestUtils
{
    public static long[] compress(LongCODEC codec, long[] data)
    {
        long[] outBuf = new long[data.Length * 8];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.compress(data, inPos, data.Length, outBuf, outPos);
        long[] answer = new long[outPos.get()];
        Array.Copy(outBuf, answer, answer.Length);
        return answer;
    }

    public static byte[] compress(ByteLongCODEC codec, long[] data)
    {
        byte[] outBuf = new byte[data.Length * 4 * 4];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.compress(data, inPos, data.Length, outBuf, outPos);
        byte[] answer = new byte[outPos.get()];
        Array.Copy(outBuf, answer, answer.Length);
        return answer;
    }

    public static long[] compressHeadless(SkippableLongCODEC codec, long[] data)
    {
        long[] outBuf = new long[codec.maxHeadlessCompressedLength(new IntWrapper(), data.Length)];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.headlessCompress(data, inPos, data.Length, outBuf, outPos);
        long[] answer = new long[outPos.get()];
        Array.Copy(outBuf, answer, answer.Length);
        return answer;
    }

    public static long[] uncompress(LongCODEC codec, long[] data, int len)
    {
        long[] outBuf = new long[len + 1024];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.uncompress(data, inPos, data.Length, outBuf, outPos);
        long[] answer = new long[outPos.get()];
        Array.Copy(outBuf, answer, answer.Length);
        return answer;
    }

    public static long[] uncompress(ByteLongCODEC codec, byte[] data, int len)
    {
        long[] outBuf = new long[len + 1024];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.uncompress(data, inPos, data.Length, outBuf, outPos);
        long[] answer = new long[outPos.get()];
        Array.Copy(outBuf, answer, answer.Length);
        return answer;
    }

    public static long[] uncompressHeadless(SkippableLongCODEC codec, long[] data, int len)
    {
        long[] outBuf = new long[len + 1024];
        IntWrapper inPos = new IntWrapper();
        IntWrapper outPos = new IntWrapper();
        codec.headlessUncompress(data, inPos, data.Length, outBuf, outPos, len);
        if (outPos.get() < len) throw new InvalidOperationException("Insufficient output.");
        long[] answer = new long[outPos.get()];
        Array.Copy(outBuf, answer, answer.Length);
        return answer;
    }

}