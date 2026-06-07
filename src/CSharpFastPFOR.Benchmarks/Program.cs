namespace Genbox.CSharpFastPFOR.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Benchmark.main([]);
            return;
        }

        switch (args[0])
        {
            case "benchmark":
                Benchmark.main([]);
                break;
            case "bitpacking":
                BenchmarkBitPacking.main([]);
                break;
            case "longbitpacking":
                BenchmarkLongBitPacking.main([]);
                break;
            case "vectorbitpacking":
                VectorIntBitPackBenchmark.main([]);
                break;
            case "csv":
                BenchmarkCSV.main(args[1..]);
                break;
            case "offsetted":
                BenchmarkOffsettedSeries.main([]);
                break;
            case "skippable":
                BenchmarkSkippable.main([]);
                break;
            default:
                Console.WriteLine("Usage: CSharpFastPFOR.Benchmarks <benchmark|bitpacking|longbitpacking|vectorbitpacking|csv|offsetted|skippable> [csv options/files]");
                break;
        }
    }
}