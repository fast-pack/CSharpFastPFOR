/**
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 */

/**
 * PerformanceLogger for IntegerCODEC.
 *
 * @author MURAOKA Taro http://github.com/koron
 */
namespace Genbox.CSharpFastPFOR.Benchmarks;

public class PerformanceLogger
{
    internal class Timer
    {
        private long startNano;
        private long duration = 0;

        public void start()
        {
            this.startNano = Port.System.nanoTime();
        }

        public long end()
        {
            return this.duration += Port.System.nanoTime()- this.startNano;
        }

        public long getDuration()
        {
            return this.duration;
        }
    }

   internal readonly Timer compressionTimer = new Timer();
   internal readonly Timer decompressionTimer = new Timer();

    private long originalSize = 0;
    private long compressedSize = 0;

    public long addOriginalSize(long value)
    {
        return this.originalSize += value;
    }

    public long addCompressedSize(long value)
    {
        return this.compressedSize += value;
    }

    private long getOriginalSize()
    {
        return this.originalSize;
    }

    private long getCompressedSize()
    {
        return this.compressedSize;
    }

    public double getBitPerInt()
    {
        return this.compressedSize * 32.0 / this.originalSize;
    }

    private static double getMiS(long size, long nanoTime)
    {
        return (size * 1e-6) / (nanoTime * 1.0e-9);
    }

    public double getCompressSpeed()
    {
        return getMiS(this.originalSize, this.compressionTimer.getDuration());
    }

    public double getDecompressSpeed()
    {
        return getMiS(this.originalSize, this.decompressionTimer.getDuration());
    }
}