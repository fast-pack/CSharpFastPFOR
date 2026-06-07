using System.Diagnostics;

namespace Genbox.CSharpFastPFOR.Benchmarks.Port;

internal static class System
{
    public static long nanoTime() => (long)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000000000.0));
}