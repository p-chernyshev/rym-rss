namespace RymRss.Extensions;

public static class LinqExtensions
{
    public static IEnumerable<IEnumerable<TElement>> SplitBy<TElement>(
        this IEnumerable<TElement> source,
        Predicate<TElement> predicate,
        bool skipEmptyGroups = true)
    {
        var group = new List<TElement>();

        foreach (var item in source)
        {
            if (predicate(item))
            {
                if (group.Count == 0 && skipEmptyGroups) continue;
                yield return group;
                group = new List<TElement>();
            }
            else
            {
                group.Add(item);
            }
        }

        if (group.Count > 0 || !skipEmptyGroups) yield return group;
    }
}
