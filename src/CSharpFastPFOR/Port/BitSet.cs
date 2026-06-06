using System.Collections;

namespace Genbox.CSharpFastPFOR.Port;

public class BitSet
{
    private BitArray _bitArray;

    public BitSet(int max)
    {
        _bitArray = new BitArray(max);
    }

    public bool get(int i)
    {
        return _bitArray[i];
    }

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