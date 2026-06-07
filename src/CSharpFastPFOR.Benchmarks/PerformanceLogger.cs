/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

/*
 * PerformanceLogger for IntegerCODEC.
 *
 * @author MURAOKA Taro http://github.com/koron
 */
namespace Genbox.CSharpFastPFOR.Benchmarks;

internal class PerformanceLogger
{
    internal readonly Timer compressionTimer = new Timer();
    internal readonly Timer decompressionTimer = new Timer();
    private long compressedSize;

    private long originalSize;

    internal void addOriginalSize(long value)
    {
        originalSize += value;
    }

    internal void addCompressedSize(long value)
    {
        compressedSize += value;
    }

    internal double getBitPerInt() => (compressedSize * 32.0) / originalSize;

    private static double getMiS(long size, long nanoTime) => (size * 1e-6) / (nanoTime * 1.0e-9);

    internal double getCompressSpeed() => getMiS(originalSize, compressionTimer.getDuration());

    internal double getDecompressSpeed() => getMiS(originalSize, decompressionTimer.getDuration());

    internal class Timer
    {
        private long duration;
        private long startNano;

        internal void start()
        {
            startNano = Port.System.nanoTime();
        }

        internal void end()
        {
            duration += Port.System.nanoTime() - startNano;
        }

        internal long getDuration() => duration;
    }
}