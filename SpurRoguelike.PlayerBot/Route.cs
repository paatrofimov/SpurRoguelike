using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    internal class Route : List<Location>
    {

        public Route() {}

        public Route(Location location) : base(EnumerableUtil.FromSingleItem(location)) { }

        public Route(IEnumerable<Location> locations) : base(locations) { }

        public Route(Route route, Location node) : base(route)
        {
            Add(node);
        }

        public Route MergeWith(Route with)
        {
            var mergeIndex = with.IndexOf(this.Last()) + 1;
            return new Route(
                this.Concat(with.Skip(mergeIndex)));
        }
        
        public List<Route> MakeDirectBreadthFirstSearch(IEnumerable<Location> neighbours)
        {
            var searched = neighbours
                .Where(l => !this.Contains(l) && !BreadthFirstSearchAlgorithm.DirectSearchedSteps.Contains(l))
                .Select(l => new Route(this, l))
                .ToList();

            foreach (var l in neighbours)
                BreadthFirstSearchAlgorithm.DirectSearchedSteps.Add(l);

            return searched;
        }

        public List<Route> MakeReverseBreadthFirstSearch(IEnumerable<Location> neighbours)
        {
            var searched = neighbours
                .Where(l => !this.Contains(l) && !BreadthFirstSearchAlgorithm.ReverseSearchedSteps.Contains(l))
                .Select(l => new Route(this, l))
                .ToList();

            foreach (var l in neighbours)
                BreadthFirstSearchAlgorithm.ReverseSearchedSteps.Add(l);

            return searched;
        }
    }
}