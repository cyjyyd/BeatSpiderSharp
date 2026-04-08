namespace BeatSpiderSharp.Extensions;

public static class CollectionExtensions
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        if (collection is List<T> list)
        {
            list.AddRange(items);
            return;
        }

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    public static void ReplaceWith<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        collection.AddRange(items);
    }
}
