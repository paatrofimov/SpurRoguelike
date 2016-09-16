using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal static class PawnViewUtil
    {
        public static IEnumerable<PawnView> GetMonstersAround(this IEnumerable<PawnView> monsters, Location around)
        {
            return monsters.Where(m => Offset.AttackOffsets.Any(o => m.Location + o == around));
        }
        
        public static IEnumerable<PawnView> GetMonstersOnRoute(this IEnumerable<PawnView> amongMonsters, Route route)
        {
            return amongMonsters.Where(m => route.Skip(1).Any(l => Offset.AttackOffsets.Any(o => m.Location == l + o)));
        }

        public static int CountMonstersOnRoute(this IEnumerable<PawnView> amongMonsters, Route route)
        {
            return amongMonsters.Count(m => Offset.AttackOffsets.Any(o => route.Skip(1).Any(l => l + o == m.Location)));
        }
    }
}