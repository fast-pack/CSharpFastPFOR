namespace Genbox.CSharpFastPFOR.IntegerCompression.Port;

public class ByteBuffer : IDisposable
{
    private readonly BinaryReader _br;
    private readonly BinaryWriter _bw;
    public readonly MemoryStream _ms;
    private ByteOrder _order;
    private bool _disposed;

    public ByteBuffer(int length)
    {
        _ms = new MemoryStream();
        _ms.SetLength(length);

        _bw = new BinaryWriter(_ms);
        _br = new BinaryReader(_ms);

        _order = ByteOrder.BIG_ENDIAN; //To simulate Java
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _br.Dispose();
            _bw.Dispose();
            _ms.Dispose();
        }

        _disposed = true;
    }

    internal static ByteBuffer allocateDirect(int length) => new ByteBuffer(length);

    public void order(ByteOrder order)
    {
        _order = order;
    }

    public int position() => (int)_ms.Position;

    public void put(sbyte b)
    {
        _bw.Write(b);
    }

    public void flip()
    {
        _ms.Position = 0;
    }

    public IntBuffer asIntBuffer() => new IntBuffer(_ms, _order);

    public LongBuffer asLongBuffer() => new LongBuffer(_ms, _order);

    public void clear()
    {
        _ms.Position = 0;
    }

    public sbyte get() => _br.ReadSByte();
}