using System.Collections.Generic;

namespace SpurRoguelike.PlayerBot
{
    internal static class EnumerableUtil
    {
        public static IEnumerable<T> FromSingleItem<T>(T item)
        {
            yield return item;
        }
    }
}