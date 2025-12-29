namespace Mars.Core.Interfaces;

public interface ICanCopy<T> where T : class
{
    public T Copy(T instance);
}
