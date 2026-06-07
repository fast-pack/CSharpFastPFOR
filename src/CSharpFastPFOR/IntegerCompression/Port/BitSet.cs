using System.Collections;

namespace Genbox.CSharpFastPFOR.IntegerCompression.Port;

public class BitSet(int max)
{
    private readonly BitArray _bitArray = new BitArray(max);

    public bool get(int i) => _bitArray[i];

    public void set(int i)
    {
        _bitArray[i] = true;
    }

    public int nextSetBit(int fromIndex)
    {
        for (int j = fromIndex; j < _bitArray.Length; j++)
        {
            if (_bitArray[j])
                return j;
        }

        return -1;
    }
}