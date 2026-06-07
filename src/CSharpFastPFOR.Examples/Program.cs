using Genbox.CSharpFastPFOR.Examples.Axelbrooke;
using Genbox.CSharpFastPFOR.Examples.Vector;

namespace Genbox.CSharpFastPFOR.Examples;

internal static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
            example.main(args);
        else if (args[0] == "vector")
            Example.main(args[1..]);
        else if (args[0] == "bitmap")
            CompressBitmap.main(args[1..]);
        else
            example.main(args);
    }
}