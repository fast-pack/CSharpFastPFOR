// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CA1034 // LaneWidth intentionally mirrors Java's nested interface enum.

using System;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Vector;

/// <summary>Width-specific vector bit-packing kernels for a 256-integer block.</summary>
public interface VectorBitPackerKernels
{
    void fastpack(int[] input, int inpos, int[] output, int outpos, int b);
    void fastpackNoMask(int[] input, int inpos, int[] output, int outpos, int b);
    void fastunpack(int[] input, int inpos, int[] output, int outpos, int b);

    /// <summary>Hardware vector width carried by vector-compressed streams.</summary>
    public sealed class LaneWidth
    {
        public static readonly LaneWidth BITS_128 = new LaneWidth(0, 128, new VectorBitPacker128());
        public static readonly LaneWidth BITS_256 = new LaneWidth(1, 256, new VectorBitPacker256());
        public static readonly LaneWidth BITS_512 = new LaneWidth(2, 512, new VectorBitPacker());

        public static readonly LaneWidth PREFERRED = forHost(IntVector.PreferredVectorBits);

        private LaneWidth(int code, int bits, VectorBitPackerKernels kernel)
        {
            Code = code;
            Bits = bits;
            Kernel = kernel;
        }

        public int Code { get; }
        public int Bits { get; }
        public VectorBitPackerKernels Kernel { get; }

        public static LaneWidth forHost(int hostBits)
        {
            if (hostBits >= BITS_512.Bits) return BITS_512;
            if (hostBits >= BITS_256.Bits) return BITS_256;
            if (hostBits >= BITS_128.Bits) return BITS_128;
            throw new InvalidOperationException("no vector bit-packing kernel fits this machine's preferred vector width of " + hostBits + " bits");
        }

        public static LaneWidth fromCode(int code) => code switch
        {
            0 => BITS_128,
            1 => BITS_256,
            2 => BITS_512,
            _ => throw new ArgumentException("unknown vector lane-width tag " + code, nameof(code))
        };
    }
}