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

    public static bool EquivalentBy<TElement, TKey>(
        this IEnumerable<TElement> first,
        IEnumerable<TElement> second,
        Func<TElement, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        var firstList = first.ToList();
        var secondList = second.ToList();

        if (firstList.Count != secondList.Count) return false;

        var unionCount = firstList
            .UnionBy(secondList, keySelector, comparer)
            .Count();

        return firstList.Count == unionCount;
    }
}
