using Genbox.CSharpFastPFOR.Port;

namespace Genbox.CSharpFastPFOR.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkMethod();
        BenchmarkBitPackingMethod();
        BenchmarkCSVMethod(args);
        BenchmarkOffsettedSeriesMethod();
        BenchmarkSkippableMethod();
    }

    private static void BenchmarkMethod()
    {
        Console.WriteLine("# benchmark based on the ClusterData model from:");
        Console.WriteLine("# 	 Vo Ngoc Anh and Alistair Moffat. ");
        Console.WriteLine("#	 Index compression using 64-bit words.");
        Console.WriteLine("# 	 Softw. Pract. Exper.40, 2 (February 2010), 131-147. ");
        Console.WriteLine();

        string path = Path.GetFullPath("benchmark-" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv");

        using (FileStream csvFile = File.OpenWrite(path))
        using (StreamWriter writer = new StreamWriter(csvFile))
        {
            Console.WriteLine("# Results will be written into a CSV file: " + path);
            Console.WriteLine();
            Benchmark.test(writer, 20, 18, 10);
            Console.WriteLine();
            Console.WriteLine("Results were written into a CSV file: " + path);
        }
    }

    private static void BenchmarkBitPackingMethod()
    {
        Console.WriteLine("Testing packing and delta ");
        BenchmarkBitPacking.testWithDeltas(false);
        BenchmarkBitPacking.testWithDeltas(true);
        Console.WriteLine("Testing packing alone ");
        BenchmarkBitPacking.test(false);
        BenchmarkBitPacking.test(true);
    }

    private static void BenchmarkCSVMethod(string[] args)
    {
        Format myformat = Format.ONEARRAYPERLINE;
        CompressionMode cm = CompressionMode.DELTA;
        List<string> files = [];
        foreach (string s in args)
        {
            if (s.StartsWith("-"))
            {// it is a flag
                if (s.Equals("--onearrayperfile"))
                    myformat = Format.ONEARRAYPERFILE;
                else if (s.Equals("--nodelta"))
                    cm = CompressionMode.AS_IS;
                else if (s.Equals("--oneintperline"))
                    myformat = Format.ONEINTPERLINE;
                else
                    throw new Exception("I don't understand: " + s);
            }
            else
            {// it is a filename
                files.Add(s);
            }
        }
        if (myformat == Format.ONEARRAYPERFILE)
            Console.WriteLine("Treating each file as one array.");
        else if (myformat == Format.ONEARRAYPERLINE)
            Console.WriteLine("Each line of each file is an array: use --onearrayperfile or --oneintperline to change.");
        else if (myformat == Format.ONEINTPERLINE)
            Console.WriteLine("Treating each file as one array, with one integer per line.");
        if (cm == CompressionMode.AS_IS)
            Console.WriteLine("Compressing the integers 'as is' (no differential coding)");
        else
            Console.WriteLine("Using differential coding (arrays will be sorted): use --nodelta to prevent sorting");

        List<int[]> data = [];
        foreach (string fn in files)
        foreach (int[] x in BenchmarkCSV.loadIntegers(fn, myformat))
            data.Add(x);
        Console.WriteLine("Loaded " + data.Count + " array(s)");
        if (cm == CompressionMode.DELTA)
        {
            Console.WriteLine("Sorting the arrray(s) because you are using differential coding");
            foreach (int[] x in data)
                Arrays.sort(x);
        }
        BenchmarkCSV.bench(data, cm, false);
        BenchmarkCSV.bench(data, cm, false);
        BenchmarkCSV.bench(data, cm, true);
        BenchmarkCSV.bytebench(data, cm, false);
        BenchmarkCSV.bytebench(data, cm, false);
        BenchmarkCSV.bytebench(data, cm, true);
    }

    private static void BenchmarkOffsettedSeriesMethod()
    {
        string path = Path.GetFullPath("benchmark-offsetted-" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv");

        using (FileStream csvFile = File.OpenWrite(path))
        using (StreamWriter writer = new StreamWriter(csvFile))
        {
            Console.WriteLine("# Results will be written into a CSV file: " + path);
            Console.WriteLine();
            BenchmarkOffsettedSeries.run(writer, 8 * 1024, 1280);
            Console.WriteLine();
            Console.WriteLine("# Results were written into a CSV file: " + path);
        }
    }

    private static void BenchmarkSkippableMethod()
    {
        Console.WriteLine("# benchmark based on the ClusterData model from:");
        Console.WriteLine("# 	 Vo Ngoc Anh and Alistair Moffat. ");
        Console.WriteLine("#	 Index compression using 64-bit words.");
        Console.WriteLine("# 	 Softw. Pract. Exper.40, 2 (February 2010), 131-147. ");
        Console.WriteLine();

        string path = Path.GetFullPath("benchmark-skippable-" + DateTime.Now.ToString("yyyyMMddTHHmmss") + ".csv");

        using (FileStream csvFile = File.OpenWrite(path))
        using (StreamWriter writer = new StreamWriter(csvFile))
        {
            Console.WriteLine("# Results will be written into a CSV file: " + path);
            Console.WriteLine();
            BenchmarkSkippable.test(writer, 20, 18, 10);
            Console.WriteLine();
            Console.WriteLine("Results were written into a CSV file: " + path);
        }
    }
}