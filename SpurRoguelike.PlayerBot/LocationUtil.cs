using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    internal static class LocationUtil
    {
        public static int GetDistanceTo(this Location from, Location to)
        {
            return (to - from).Size();
        }

        public static bool IsOnStraightSafeRun(this Location from, Location to, PlayerBot playerBot,
            ref Route straightSafeRoute)
        {
            if (from.X != to.X && from.Y != to.Y)
                return false;

            straightSafeRoute = from.GetStraightRunRoute(to);

            if (straightSafeRoute.Any(l => playerBot.LevelView.Field[l] <= CellType.Trap
                                 || playerBot.SeeMonsterAt(l)
                                 || playerBot.SeeItemAt(l)))
                return false;

            const int dangerZoneRange = 2;
            var route = straightSafeRoute;
            var pendingMonsters = playerBot.LevelView.Monsters.Where(m => route.Any(l => m.Location.GetDistanceTo(l) <= dangerZoneRange));
            var hypotheticalDamageToTake = pendingMonsters.Select(m => (int)((double)m.TotalAttack / playerBot.TotalDefence * 10)).Sum();

            return hypotheticalDamageToTake < playerBot.Health;
        }

        public static Route GetStraightRunRoute(this Location from, Location to)
        {
            var locations = Enumerable.Empty<Location>();
            var stepOffset = (to - from).SnapToStep();
            var next = from + stepOffset;

            while (next != to)
            {
                locations = locations.Concat(EnumerableUtil.FromSingleItem(next));
                next += stepOffset;
            }
            //locations = locations.Concat(EnumerableUtil.FromSingleItem(to));

            return new Route(locations);
        }

        public static bool StepIsAvailable(this Location to, PlayerBot playerBot)
        {
            return playerBot.LevelView.Field[to] > CellType.Trap
                && !playerBot.SeeMonsterAt(to)
                && !playerBot.SeeItemAt(to)
                && !playerBot.SeeExitAt(to);
        }

        public static IEnumerable<Location> GetAvailableSteps(this Location from, PlayerBot playerBot)
        {
            return Offset.StepOffsets
                .Where(o => (from + o).StepIsAvailable(playerBot))
                .Select(o => from + o);
        }

        //public static Offset SnapToStep(Offset offsetStep)
        //{
        //    if (offsetStep.XOffset == 0 && offsetStep.YOffset == 0)
        //        return default(Offset);

        //    if (offsetStep.XOffset == 0)
        //        return offsetStep.YOffset > 0
        //            ? new Offset(0, 1)
        //            : new Offset(0, -1);

        //    if (offsetStep.YOffset == 0)
        //        return offsetStep.XOffset > 0
        //            ? new Offset(1, 0)
        //            : new Offset(-1, 0);

        //    if (Math.Abs(offsetStep.XOffset) >= Math.Abs(offsetStep.YOffset))
        //        return offsetStep.XOffset > 0 
        //            ? new Offset(1, 0) 
        //            : new Offset(-1, 0);

        //    if (Math.Abs(offsetStep.YOffset) > Math.Abs(offsetStep.XOffset))
        //        return offsetStep.YOffset > 0
        //            ? new Offset(0, 1)
        //            : new Offset(0, -1);

        //    return default(Offset);
        //}
    }
}