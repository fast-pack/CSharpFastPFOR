/*
 * This code is released under the
 * Apache License Version 2.0 http://www.apache.org/licenses/.
 *
 */

/*
 * Essentially a mutable wrapper around an integer.
 *
 * @author dwu
 */
using System.Globalization;

namespace Genbox.CSharpFastPFOR.IntegerCompression;

public sealed class IntWrapper(int v)
{
    private int value = v;

    /*
     * Constructor: value set to 0.
     */
    public IntWrapper() : this(0) {}

    /*
     * add the provided value to the integer
     * @param v value to add
     */
    public void add(int v)
    {
        value += v;
    }

    public double doubleValue() => value;

    public float floatValue() => value;

    /*
     * @return the integer value
     */
    public int get() => value;

    /*
     * add 1 to the integer value
     */
    public void increment()
    {
        value++;
    }

    public int intValue() => value;

    public long longValue() => value;

    /*
     * Set the value to that of the specified integer.
     *
     * @param newValue
     * specified integer value
     */
    public void set(int newValue)
    {
        value = newValue;
    }

    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
}