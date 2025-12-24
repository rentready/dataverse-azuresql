namespace RR.DataverseAzureSql.Common.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<IEnumerable<TSource>> InBatchBy<TSource>(this IEnumerable<TSource> items, int batchSize)
    {
        var batch = new List<TSource>();
        foreach (var item in items)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<TSource>();
            }
        }

        if (batch.Any()) yield return batch;
    }
}

