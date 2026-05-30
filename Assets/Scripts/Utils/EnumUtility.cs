using System;
using System.Collections.Generic;

public static class EnumUtility
{
    private static readonly Dictionary<Type, Array> _cache = new();

    public static T GetRandomEnum<T>() where T : Enum
    {
        var type = typeof(T);
        if (!_cache.TryGetValue(type, out Array values))
        {
            values = Enum.GetValues(type);
            _cache[type] = values;
        }
        
        return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }
}