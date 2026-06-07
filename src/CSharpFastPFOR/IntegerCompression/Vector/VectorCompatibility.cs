#pragma warning disable S2325, S2326 // Mirror the Java Vector API used by generated callers.
#if NET10_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
#endif

namespace Genbox.CSharpFastPFOR.IntegerCompression.Vector;

internal sealed class VectorSpecies<T>
{
    internal VectorSpecies(int length) => Length = length;
    internal int Length { get; }
}

internal enum VectorOperators
{
    LSHL,
    ASHR,
    LSHR,
    OR
}

#if NET10_0_OR_GREATER
[StructLayout(LayoutKind.Auto)]
#endif
internal readonly struct IntVector
{
#if NET10_0_OR_GREATER
    internal static int PreferredVectorBits => Vector512.IsHardwareAccelerated ? 512 : Vector256.IsHardwareAccelerated ? 256 : 128;
#else
    internal static int PreferredVectorBits => 128;
#endif

    internal static readonly VectorSpecies<int> SPECIES_128 = new VectorSpecies<int>(4);
    internal static readonly VectorSpecies<int> SPECIES_256 = new VectorSpecies<int>(8);
    internal static readonly VectorSpecies<int> SPECIES_512 = new VectorSpecies<int>(16);
    internal static readonly VectorSpecies<int> SPECIES_PREFERRED = new VectorSpecies<int>(PreferredVectorBits / 32);

#if NET10_0_OR_GREATER
    private readonly Vector128<int> _vector128;
    private readonly Vector256<int> _vector256;
    private readonly Vector512<int> _vector512;
    private readonly int _length;

    private IntVector(Vector128<int> vector) { _vector128 = vector; _vector256 = default; _vector512 = default; _length = 4; }
    private IntVector(Vector256<int> vector) { _vector128 = default; _vector256 = vector; _vector512 = default; _length = 8; }
    private IntVector(Vector512<int> vector) { _vector128 = default; _vector256 = default; _vector512 = vector; _length = 16; }

    internal static IntVector broadcast(VectorSpecies<int> species, int value) => species.Length switch
    {
        4 => new IntVector(Vector128.Create(value)),
        8 => new IntVector(Vector256.Create(value)),
        16 => new IntVector(Vector512.Create(value)),
        _ => throw new ArgumentOutOfRangeException(nameof(species))
    };

    internal static IntVector fromArray(VectorSpecies<int> species, int[] source, int offset)
    {
        ref int firstLane = ref MemoryMarshal.GetReference(source.AsSpan(offset, species.Length));
        return species.Length switch
        {
            4 => new IntVector(Vector128.LoadUnsafe(ref firstLane)),
            8 => new IntVector(Vector256.LoadUnsafe(ref firstLane)),
            16 => new IntVector(Vector512.LoadUnsafe(ref firstLane)),
            _ => throw new ArgumentOutOfRangeException(nameof(species))
        };
    }

    internal IntVector and(IntVector other) => _length switch
    {
        4 => new IntVector(Vector128.BitwiseAnd(_vector128, other._vector128)),
        8 => new IntVector(Vector256.BitwiseAnd(_vector256, other._vector256)),
        16 => new IntVector(Vector512.BitwiseAnd(_vector512, other._vector512)),
        _ => throw new InvalidOperationException("Unsupported vector length.")
    };

    internal IntVector and(int value) => and(broadcast(new VectorSpecies<int>(_length), value));

    internal IntVector or(IntVector other) => _length switch
    {
        4 => new IntVector(Vector128.BitwiseOr(_vector128, other._vector128)),
        8 => new IntVector(Vector256.BitwiseOr(_vector256, other._vector256)),
        16 => new IntVector(Vector512.BitwiseOr(_vector512, other._vector512)),
        _ => throw new InvalidOperationException("Unsupported vector length.")
    };

    internal IntVector zero() => broadcast(new VectorSpecies<int>(_length), 0);
    internal IntVector zero(VectorSpecies<int> species) => broadcast(species, 0);

    internal IntVector lanewise(VectorOperators operation, int amount) => _length switch
    {
        4 => new IntVector(operation switch { VectorOperators.LSHL => Vector128.ShiftLeft(_vector128, amount), VectorOperators.ASHR => Vector128.ShiftRightArithmetic(_vector128, amount), VectorOperators.LSHR => Vector128.ShiftRightLogical(_vector128, amount), _ => throw new ArgumentOutOfRangeException(nameof(operation)) }),
        8 => new IntVector(operation switch { VectorOperators.LSHL => Vector256.ShiftLeft(_vector256, amount), VectorOperators.ASHR => Vector256.ShiftRightArithmetic(_vector256, amount), VectorOperators.LSHR => Vector256.ShiftRightLogical(_vector256, amount), _ => throw new ArgumentOutOfRangeException(nameof(operation)) }),
        16 => new IntVector(operation switch { VectorOperators.LSHL => Vector512.ShiftLeft(_vector512, amount), VectorOperators.ASHR => Vector512.ShiftRightArithmetic(_vector512, amount), VectorOperators.LSHR => Vector512.ShiftRightLogical(_vector512, amount), _ => throw new ArgumentOutOfRangeException(nameof(operation)) }),
        _ => throw new InvalidOperationException("Unsupported vector length.")
    };

    internal int reduceLanes(VectorOperators operation)
    {
        if (operation != VectorOperators.OR) throw new ArgumentOutOfRangeException(nameof(operation));
        int result = 0;
        for (int i = 0; i < _length; i++) result |= _length switch { 4 => _vector128.GetElement(i), 8 => _vector256.GetElement(i), _ => _vector512.GetElement(i) };
        return result;
    }

    internal void intoArray(int[] destination, int offset)
    {
        ref int firstLane = ref MemoryMarshal.GetReference(destination.AsSpan(offset, _length));
        if (_length == 4) _vector128.StoreUnsafe(ref firstLane);
        else if (_length == 8) _vector256.StoreUnsafe(ref firstLane);
        else if (_length == 16) _vector512.StoreUnsafe(ref firstLane);
        else throw new InvalidOperationException("Unsupported vector length.");
    }
#else
    private readonly int[] _lanes;
    private IntVector(int[] lanes) => _lanes = lanes;

    internal static IntVector broadcast(VectorSpecies<int> species, int value)
    {
        int[] lanes = new int[species.Length];
        for (int i = 0; i < lanes.Length; i++) lanes[i] = value;
        return new IntVector(lanes);
    }

    internal static IntVector fromArray(VectorSpecies<int> species, int[] source, int offset)
    {
        int[] lanes = new int[species.Length];
        Array.Copy(source, offset, lanes, 0, lanes.Length);
        return new IntVector(lanes);
    }

    internal IntVector and(IntVector other) => Apply(other, (left, right) => left & right);
    internal IntVector and(int value) => Apply(broadcast(new VectorSpecies<int>(_lanes.Length), value), (left, right) => left & right);
    internal IntVector or(IntVector other) => Apply(other, (left, right) => left | right);
    internal IntVector zero() => broadcast(new VectorSpecies<int>(_lanes.Length), 0);
    internal IntVector zero(VectorSpecies<int> species) => broadcast(species, 0);

    internal IntVector lanewise(VectorOperators operation, int amount)
    {
        int[] lanes = new int[_lanes.Length];
        for (int i = 0; i < lanes.Length; i++) lanes[i] = operation switch { VectorOperators.LSHL => _lanes[i] << amount, VectorOperators.ASHR => _lanes[i] >> amount, VectorOperators.LSHR => _lanes[i] >>> amount, _ => throw new ArgumentOutOfRangeException(nameof(operation)) };
        return new IntVector(lanes);
    }

    internal int reduceLanes(VectorOperators operation)
    {
        if (operation != VectorOperators.OR) throw new ArgumentOutOfRangeException(nameof(operation));
        int result = 0;
        for (int i = 0; i < _lanes.Length; i++) result |= _lanes[i];
        return result;
    }

    internal void intoArray(int[] destination, int offset) => Array.Copy(_lanes, 0, destination, offset, _lanes.Length);

    private IntVector Apply(IntVector other, Func<int, int, int> operation)
    {
        int[] lanes = new int[_lanes.Length];
        for (int i = 0; i < lanes.Length; i++) lanes[i] = operation(_lanes[i], other._lanes[i]);
        return new IntVector(lanes);
    }
#endif
}