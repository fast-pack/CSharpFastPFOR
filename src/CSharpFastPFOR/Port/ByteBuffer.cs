namespace Genbox.CSharpFastPFOR.Port;

public class ByteBuffer : IDisposable
{
    public readonly MemoryStream _ms;
    private readonly BinaryWriter _bw;
    private readonly BinaryReader _br;
    private ByteOrder _order;

    public ByteBuffer(int length)
    {
        _ms = new MemoryStream();
        _ms.SetLength(length);

        _bw = new BinaryWriter(_ms);
        _br = new BinaryReader(_ms);

        _order = ByteOrder.BIG_ENDIAN; //To simulate Java
    }

    internal static ByteBuffer allocateDirect(int length)
    {
        return new ByteBuffer(length);
    }

    public void order(ByteOrder order)
    {
        _order = order;
    }

    public int position()
    {
        return (int)_ms.Position;
    }

    public void put(sbyte b)
    {
        _bw.Write(b);
    }

    public void flip()
    {
        _ms.Position = 0;
    }

    public IntBuffer asIntBuffer()
    {
        return new IntBuffer(_ms, _order);
    }

    public void clear()
    {
        byte[] empty = new byte[_ms.Length];
        _ms.Position = 0;
        _ms.Write(empty, 0, empty.Length);
        _ms.Position = 0;
    }

    public sbyte get()
    {
        return _br.ReadSByte();
    }

    internal void put(int[] src, int offset, int length)
    {
        //TODO: port this
        //checkBounds(offset, length, src.Length);
        //if (length > remaining())
        //    throw new BufferOverflowException();

        int end = offset + length;
        for (int i = offset; i < end; i++)
            _bw.Write(src[i]);
    }

    public void Dispose()
    {
        _ms.Dispose();
        _bw.Dispose();
        _br.Dispose();
    }
}