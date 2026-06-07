// Copyright (C) 2022 Intel Corporation

// SPDX-License-Identifier: Apache-2.0

using Genbox.CSharpFastPFOR.IntegerCompression;
using Genbox.CSharpFastPFOR.IntegerCompression.Port;
using Genbox.CSharpFastPFOR.IntegerCompression.Vector;

namespace Genbox.CSharpFastPFOR.Examples.Vector;

internal static class Example
{
    public static void main(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException();

        // pass 0 for Vector compressor , non-zero for default compressor
        int compressorToUse = int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);

        const int N = 1310720;
        int[] data = new int[N];

        // 2-bit data
        for (int k = 0; k < N; k += 1)
            data[k] = 3;

        // a few large values
        for (int k = 0; k < N; k += 5)
            data[k] = 100;
        for (int k = 0; k < N; k += 533)
            data[k] = 10000;

        int[] compressed = new int[N + 1024];

        Composition codec = new Composition(
            compressorToUse == 0 ? new VectorFastPFOR() : new FastPFOR(),
            new VariableByte());

        IntWrapper inputoffset = new IntWrapper(0);
        IntWrapper outputoffset = new IntWrapper(0);

        codec.compress(data, inputoffset, data.Length, compressed, outputoffset);

        Console.WriteLine("compressed unsorted integers from " +
                          ((data.Length * 4) / 1024) + "KB to " +
                          ((outputoffset.intValue() * 4) / 1024) + "KB");

        compressed = Arrays.copyOf(compressed, outputoffset.intValue());

        int[] recovered = new int[N];
        IntWrapper recoffset = new IntWrapper(0);

        codec.uncompress(compressed, new IntWrapper(0), compressed.Length,
            recovered, recoffset);

        Console.WriteLine("compressed length = " + compressed.Length +
                          ", uncompressed length = " + recoffset.intValue());

        if (Arrays.equals(data, recovered))
            Console.WriteLine("data is recovered without loss");
        else
            throw new InvalidOperationException("bug"); // could use assert

        Console.WriteLine();
    }
}