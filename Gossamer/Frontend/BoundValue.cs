namespace Gossamer.Frontend;

public class BoundValue<T>
{
    public delegate void ValueChangedHandler(T? oldValue, T? newValue);

    public event ValueChangedHandler? ValueChanged;

    T? _value;

    public T? Get() => _value;

    public void Set(T? value)
    {
        T? oldValue = _value;
        _value = value;
        ValueChanged?.Invoke(oldValue, value);
    }
}