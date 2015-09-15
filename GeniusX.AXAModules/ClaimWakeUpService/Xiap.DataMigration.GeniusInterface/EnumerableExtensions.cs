namespace Xiap.DataMigration.GeniusInterface.AXACS
{
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            for (var i = 0; i < source.Count(); i += chunkSize)
            {
                yield return source.Skip(i).Take(chunkSize);
            }
        }
    }
}
