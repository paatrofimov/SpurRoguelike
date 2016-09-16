using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    internal class BreadthFirstSearchAlgorithm
    {
        public static HashSet<Location> DirectSearchedSteps;
        public static HashSet<Location> ReverseSearchedSteps;
        private static IEnumerable<Location> Intersection;

        public static Route GetRouteTo(PlayerBot playerBot, Location target)
        {
            var targets = EnumerableUtil.FromSingleItem(target);
            ;

            DirectSearchedSteps = new HashSet<Location> {playerBot.Location};
            ReverseSearchedSteps = new HashSet<Location> {target};

            var initialRoutesFrom = EnumerableUtil.FromSingleItem(new Route(playerBot.Location));
            var initialRoutesTo = EnumerableUtil.FromSingleItem(new Route(target));

            if (playerBot.SeeMonsterAt(target))
            {
                targets = Offset.AttackOffsets
                    .Select(o => target + o)
                    .Where(l => l.StepIsAvailable(playerBot));
                initialRoutesTo = targets.Select(l => new Route(l));
            }

            if (!targets.Any())
                return new Route();

            if (targets.Any(l => playerBot.Location == l))
                return new Route(playerBot.Location);

            if (targets.Any(l => playerBot.Location.GetDistanceTo(l) == 1))
                return new Route(targets.First(l => playerBot.Location.GetDistanceTo(l) == 1));

            var straightSafeRoute = default(Route);
            if (initialRoutesTo.Any(r => r.Any(l => playerBot.CanMakeStraightSafeRunTo(l, ref straightSafeRoute))))
                return straightSafeRoute;

            var route = MakeBreadthFirstSearch(playerBot, initialRoutesFrom, initialRoutesTo);

            return route;
        }

        private static Route MakeBreadthFirstSearch(PlayerBot playerBot, IEnumerable<Route> directRoutes,
            IEnumerable<Route> reverseRoutes)
        {
            var directLevelRoutes = Enumerable.Empty<Route>();
            var reverseLevelRoutes = Enumerable.Empty<Route>();
            var target = reverseRoutes.First().First();

            foreach (var dr in directRoutes)
            {
                var neighbors = dr.Last().GetAvailableSteps(playerBot);
                var searchedRoutes = dr.MakeDirectBreadthFirstSearch(neighbors);
                directLevelRoutes = directLevelRoutes.Concat(searchedRoutes);
            }

            foreach (var rr in reverseRoutes)
            {
                var neighbors = rr.Last().GetAvailableSteps(playerBot);
                var searchedRoutes = rr.MakeReverseBreadthFirstSearch(neighbors);
                reverseLevelRoutes = reverseLevelRoutes.Concat(searchedRoutes);
            }

            if (RoutesHaveIntersected()
                && directLevelRoutes.Any(r => Intersection.Contains(r.Last()))
                && reverseLevelRoutes.Any(r => Intersection.Contains(r.Last())))
            {
                var directRoute = directLevelRoutes
                    .Where(dr => Intersection.Contains(dr.Last())
                                 && reverseLevelRoutes.Any(rr => rr.Contains(dr.Last())))
                    .OrderBy(dr => playerBot.LevelView.Monsters.CountMonstersOnRoute(dr))
                    .ThenBy(dr =>
                            Math.Abs((target - playerBot.Location).XOffset) >=
                            Math.Abs((target - playerBot.Location).YOffset)
                                ? target.Y - dr[1].Y
                                : target.X - dr[1].X)
                    .First();

                var reverseRoute = reverseLevelRoutes
                    .Where(rr => rr.Any(directRoute.Contains))
                    .OrderBy(rr => playerBot.LevelView.Monsters.CountMonstersOnRoute(rr))
                    .First();

                reverseRoute.Reverse();

                return new Route(directRoute.MergeWith(reverseRoute).Skip(1));
            }

            if (directLevelRoutes.Any() && reverseLevelRoutes.Any())
                return MakeBreadthFirstSearch(playerBot, directLevelRoutes, reverseLevelRoutes);

            return new Route();
        }

        private static bool RoutesHaveIntersected()
        {
            Intersection = ReverseSearchedSteps.Intersect(DirectSearchedSteps);
            return Intersection.Any();
        }
    }
}