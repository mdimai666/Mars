using System.Collections.Generic;

public class DictionaryWithDefault<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
{
    TValue _default = default!;
    public TValue DefaultValue
    {
        get { return _default; }
        set { _default = value; }
    }
    public DictionaryWithDefault() : base() { }

    public DictionaryWithDefault(IEqualityComparer<TKey> comparer) : base(comparer) { }
    public DictionaryWithDefault(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }

    public DictionaryWithDefault(TValue defaultValue) : base()
    {
        _default = defaultValue;
    }
    public new TValue this[TKey key]
    {
        get
        {
            //TValue t;
            return base.TryGetValue(key, out var t) ? t : _default;
        }
        set { base[key] = value; }
    }
}
