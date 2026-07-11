using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.Preset.FilterOptions;

public class Option
{
    [JsonProperty(Order = -99)]
    public bool Enable { get; set; }

    public static implicit operator bool(Option? option) => option is { Enable: true };
}

public class Option<T>(T initialValue) : Option
{
    [JsonProperty(Order = -89)]
    public T Filter { get; set; } = initialValue;
}

public class CollectionOption<TCollection, TValue>(TCollection initialValue)
    : Option where TCollection : ICollection<TValue>
{
    [JsonProperty(Order = -89)]
    public TCollection Filter { get; init; } = initialValue;
}

public class RangeOption<T> : Option where T : struct, IComparable<T>
{
    public T? Min { get; set; }
    public T? Max { get; set; }

    public bool InRange(T? value) => value.HasValue && (!Min.HasValue || value.Value.CompareTo(Min.Value) >= 0) &&
                                     (!Max.HasValue || value.Value.CompareTo(Max.Value) <= 0);
}

public abstract class ValueSetOption<T>(IEqualityComparer<T>? comparer = null)
    : CollectionOption<ISet<T>, T>(new HashSet<T>(comparer));
//TODO Unit tests
public abstract class LogicSetOption<T>(IEqualityComparer<T>? comparer = null) : ValueSetOption<T>(comparer)
{
    [JsonProperty(Order = -79)]
    public bool IsOr { get; set; }

    protected bool TestInclude(ICollection<T> values) => IsOr ? Filter.Overlaps(values) : Filter.IsSubsetOf(values);

    protected abstract bool TestLogic(ICollection<T> values);

    public bool SatisfiedBy(ICollection<T> values) => Filter.Count == 0 || TestLogic(values);
}

public class LogicIncludeOption<T>(IEqualityComparer<T>? comparer = null) : LogicSetOption<T>(comparer)
{
    protected override bool TestLogic(ICollection<T> values) => TestInclude(values);
}

public class LogicExcludeOption<T>(IEqualityComparer<T>? comparer = null) : LogicSetOption<T>(comparer)
{
    protected override bool TestLogic(ICollection<T> values) => !TestInclude(values);
}

/**
 * Use to test at least one of the required values is present
 */
public class IncludeOption<T>(IEqualityComparer<T>? comparer = null) : ValueSetOption<T>(comparer)
{
    public bool SatisfiedBy(T value) => Filter.Count == 0 || Filter.Contains(value);
}

/**
 * Use to test none of the excluded values is present
 */
public class ExcludeOption<T>(IEqualityComparer<T>? comparer = null) : ValueSetOption<T>(comparer)
{
    public bool SatisfiedBy(ICollection<T> values)
    {
        var excluded = Filter;
        if (excluded.Count == 0) return true; // excluding nothing
        return !excluded.Overlaps(values);
    }
}
