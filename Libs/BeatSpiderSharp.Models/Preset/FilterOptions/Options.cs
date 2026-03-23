using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.Preset.FilterOptions;

public class Option
{
    [JsonProperty(Order = -99)]
    public bool Enable { get; set; }

    public static implicit operator bool(Option option) => option.Enable;
}

public class Option<T>(T initialValue) : Option
{
    [JsonProperty(Order = -89)]
    public T Filter { get; set; } = initialValue;
}

public class RangeOption<T> : Option where T : struct, IComparable<T>
{
    public T? Min { get; set; }
    public T? Max { get; set; }

    public bool InRange(T? value) => value.HasValue && (!Min.HasValue || value.Value.CompareTo(Min.Value) >= 0) &&
                                     (!Max.HasValue || value.Value.CompareTo(Max.Value) <= 0);
}

public abstract class ValueSetOption<T>(IEqualityComparer<T>? comparer = null)
    : Option<ISet<T>>(new HashSet<T>(comparer));

public class LogicIncludeOption<T>(IEqualityComparer<T>? comparer = null) : ValueSetOption<T>(comparer)
{
    public bool IsOr { get; set; }

    public bool SatisfiedBy(ICollection<T> values)
    {
        var required = Filter;
        if (required.Count == 0) return true; // vacuously true
        return IsOr ? required.Overlaps(values) : required.IsSubsetOf(values);
    }
}

/**
 * Use to test at least one of the required values is present
 */
public class IncludeOption<T>(IEqualityComparer<T>? comparer = null) : ValueSetOption<T>(comparer)
{
    public bool SatisfiedBy(T value) => Filter.Count == 0 || Filter.Contains(value);
}

public class ExcludeOption<T>(IEqualityComparer<T>? comparer = null) : ValueSetOption<T>(comparer)
{
    public bool SatisfiedBy(ICollection<T> values)
    {
        var excluded = Filter;
        if (excluded.Count == 0) return true; // excluding nothing
        return !excluded.Overlaps(values);
    }
}
