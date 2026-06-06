/**
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 */

/**
 * Essentially a mutable wrapper around an integer.
 *
 * @author dwu
 */
namespace Genbox.CSharpFastPFOR;

public class IntWrapper
{
    private int value;

    /**
     * Constructor: value set to 0.
     */
    public IntWrapper() : this(0)
    {
    }

    /**
     * Construction: value set to provided argument.
     *
     * @param v
     *                value to wrap
     */
    public IntWrapper(int v)
    {
        this.value = v;
    }

    /**
     * add the provided value to the integer
     * @param v value to add
     */
    public void add(int v)
    {
        this.value += v;
    }


    public double doubleValue()
    {
        return this.value;
    }

    public float floatValue()
    {
        return this.value;
    }

    /**
     * @return the integer value
     */
    public int get()
    {
        return this.value;
    }

    /**
     * add 1 to the integer value
     */
    public void increment()
    {
        this.value++;
    }

    public int intValue()
    {
        return this.value;
    }

    public long longValue()
    {
        return this.value;
    }

    /**
     * Set the value to that of the specified integer.
     *
     * @param value
     *                specified integer value
     */
    public void set(int value)
    {
        this.value = value;
    }

    public override string ToString()
    {
        return value.ToString();
    }
}