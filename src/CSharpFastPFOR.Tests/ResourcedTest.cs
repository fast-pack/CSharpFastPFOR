using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Differential;

namespace Genbox.CSharpFastPFOR.Tests;

public class ResourcedTest
{
    private readonly SkippableIntegerCODEC[] codecs =
    [
        new JustCopy(),
        new VariableByte(),
        new SkippableComposition(new BinaryPacking(), new VariableByte()),
        new SkippableComposition(new NewPFD(), new VariableByte()),
        new SkippableComposition(new NewPFDS9(), new VariableByte()),
        new SkippableComposition(new NewPFDS16(), new VariableByte()),
        new SkippableComposition(new OptPFD(), new VariableByte()),
        new SkippableComposition(new OptPFDS9(), new VariableByte()),
        new SkippableComposition(new OptPFDS16(), new VariableByte()),
        new SkippableComposition(new FastPFOR128(), new VariableByte()),
        new SkippableComposition(new FastPFOR(), new VariableByte()),
        new Simple9(),
        new Simple16()
    ];

    [Fact]
    public void IntCompressorTest()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Resources", "integers.txt");
        int[] data = File.ReadLines(path).Select(int.Parse).ToArray();

        IntegratedIntCompressor integrated = new IntegratedIntCompressor();
        int[] compressed = integrated.compress(data);
        int[] recovered = integrated.uncompress(compressed);
        Assert.Equal(data, recovered);

        foreach (SkippableIntegerCODEC codec in codecs)
        {
            IntCompressor compressor = new IntCompressor(codec);
            compressed = compressor.compress(data);
            recovered = compressor.uncompress(compressed);
            Assert.Equal(data, recovered);
        }

        foreach (SkippableIntegerCODEC codec in codecs)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (codec is SkippableIntegratedIntegerCODEC integratedCodec)
            {
                IntegratedIntCompressor compressor = new IntegratedIntCompressor(integratedCodec);
                compressed = compressor.compress(data);
                recovered = compressor.uncompress(compressed);
                Assert.Equal(data, recovered);
            }
        }
    }
}