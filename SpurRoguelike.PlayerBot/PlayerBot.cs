using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal class PlayerBot
    {
        private Turn _nextTurn;
        public Turn NextTurn
        {
            get { return _nextTurn ?? (_nextTurn = Turn.None); }
            set { _nextTurn = value; }
        }
        public LevelView LevelView;
        private State<PlayerBot> State;
        private PawnView TargetMonster;
        private IView Objective;
        private ItemView EquippedItem;
        public Location Location;
        private int TotalAttack;
        public int TotalDefence;
        public int Health;
        private const int HealthMaximum = 100;

        public void Refresh(LevelView levelView)
        {
            LevelView = levelView;
            Health = levelView.Player.Health;
            Location = levelView.Player.Location;
            TotalAttack = levelView.Player.TotalAttack;
            TotalDefence = levelView.Player.TotalDefence;
            levelView.Player.TryGetEquippedItem(out EquippedItem);
            Objective = default(IView);

            if (TargetMonster.IsDestroyed 
                || LevelView.Monsters.Any(m => Location.GetDistanceTo(m.Location) <= 2 && m.Location != TargetMonster.Location))
                TargetMonster = default(PawnView);
        }

        public void Tick()
        {
            State = new StateIdle(this);
            State.Tick();
        }

        public bool IsAtLastLevel()
        {
            var exit = LevelView.Field.GetCellsOfType(CellType.Exit).First();
            return Offset.AttackOffsets.All(o => LevelView.Field[exit + o] == CellType.Wall);
        }

        public bool SeeExitAt(Location at)
        {
            return LevelView.Field[at] == CellType.Exit;
        }

        public bool IsCorneredAt(Location at)
        {
            return at.GetAvailableSteps(this).Count() == 1;
        }

        public bool TryAvoidCornering(ref Offset escapeStep)
        {
            if (!IsCorneredAt(Location))
                return false;

            escapeStep = Offset.StepOffsets
                .Where(o => (Location + o).StepIsAvailable(this))
                .FirstOrDefault();

            return escapeStep != default(Offset);
        }

        private bool SeeHealthAt(Location at)
        {
            return LevelView.GetHealthPackAt(at).HasValue;
        }

        private bool CanGrabHealth(ref HealthPackView health)
        {
            if (Offset.StepOffsets.Any(o => SeeHealthAt(Location + o)))
                health = LevelView.GetHealthPackAt(Location + Offset.StepOffsets.First(o => SeeHealthAt(Location + o)));
            else
                return false;

            return true;
        }

        private HealthPackView GetHealthPackWithSafestRoute(out Route safestRoute)
        {
            var parameterizedRoutes = new List< Tuple<Route, HealthPackView, int> >();
            var healthPacks = LevelView.HealthPacks.OrderBy(hp => Location.GetDistanceTo(hp.Location));

            foreach (var hp in healthPacks)
            {
                var route = BreadthFirstSearchAlgorithm.GetRouteTo(this, hp.Location);

                if (!route.Any())
                    continue;

                var countRouteMonsters = LevelView.Monsters.CountMonstersOnRoute(route);

                if (countRouteMonsters == 0)
                {
                    safestRoute = route;
                    return hp;
                }

                parameterizedRoutes.Add(new Tuple<Route, HealthPackView, int>(route, hp, countRouteMonsters));
            }

            if (parameterizedRoutes.Count == 0)
            {
                safestRoute = new Route();
                return default(HealthPackView);
            }

            safestRoute = parameterizedRoutes
                .OrderBy(r => r.Item3)
                .ThenBy(r => r.Item1.Count)
                .Select(r => r.Item1)
                .First();

            return parameterizedRoutes
                .OrderBy(r => r.Item3)
                .ThenBy(r => r.Item1.Count)
                .Select(r => r.Item2)
                .First();
        }

        public bool SeeItemAt(Location at)
        {
            return LevelView.GetItemAt(at).HasValue;
        }

        private bool CanGrabItem(ref ItemView item)
        {
            if (Offset.StepOffsets.Any(o => SeeItemAt(Location + o)))
            {
                item = LevelView.Items.Where(i => Offset.StepOffsets.Any(o => i.Location == Location + o))
                    .OrderByDescending(i => i.GetItemValue())
                    .First();
            }
            else
                return false;

            return true;
        }

        private bool TrySetObjective(bool needHealth, bool needItem, ref Route objectiveRoute)
        {
            if (!needItem && !needHealth)
                return false;

            var item = default(ItemView);
            if (needItem)
                item = LevelView.Items.FindBestItem();

            var health = default(HealthPackView);
            if (needHealth)
                health = GetHealthPackWithSafestRoute(out objectiveRoute);

            if (needItem && !item.HasValue
                || needHealth && !health.HasValue)
                return false;

            if (needItem)
                if (!EquippedItem.HasValue 
                    || item.GetItemValue() > EquippedItem.GetItemValue())
                {
                    Objective = item;
                    objectiveRoute = BreadthFirstSearchAlgorithm.GetRouteTo(this, item.Location);
                }
                else
                    return false;
            else
                Objective = health;

            return true;
        }

        public bool SeeMonsterAt(Location at)
        {
            return LevelView.GetMonsterAt(at).HasValue;
        }

        private bool IsInAttackRange(PawnView other)
        {
            return Offset.AttackOffsets.Any(o => Location + o == other.Location);
        }

        private bool TrySetTargetMonster()
        {
            if (TargetMonster.HasValue)
                return true;

            if (LevelView.Monsters.Any(IsInAttackRange))
            {
                TargetMonster = LevelView.Monsters
                    .Where(IsInAttackRange)
                    .OrderBy(m => m.Health)
                    .ThenBy(m => m.Attack)
                    .ThenBy(m => m.Defence)
                    .First();
                return true;
            }

            TargetMonster = LevelView.Monsters
                .OrderBy(m => Location.GetDistanceTo(m.Location))
                .ThenBy(m => m.Health)
                .ThenBy(m => m.Attack)
                .ThenBy(m => m.Defence)
                .FirstOrDefault();

            return TargetMonster.HasValue;
        }

        private bool IsFitnessedToFightWith(PawnView monster)
        {
            return IsFitnessedToFightWith(EnumerableUtil.FromSingleItem(monster));
        }

        private bool IsFitnessedToFightWith(IEnumerable<PawnView> crowd)
        {
            var rangedMonsters = crowd
                .OrderBy(m => m.Health).
                Select(m => new Tuple<int, int, int>(m.Health, m.TotalAttack, m.TotalDefence))
                .AsEnumerable();

            var weakestMonster = rangedMonsters.First();

            var predictedDamageToMonster = (int)(((double)TotalAttack / weakestMonster.Item3) * 9);

            var predictedDamageToSelf = rangedMonsters
                .Sum(monster => (int)(((double)monster.Item2 / TotalDefence) * 10));

            if (predictedDamageToMonster >= weakestMonster.Item1)
            {
                predictedDamageToSelf = rangedMonsters
                    .Skip(1)
                    .Sum(monster => (int)(((double)monster.Item2 / TotalDefence) * 10));

                if (predictedDamageToSelf > Health)
                    return false;
            }

            var hitsToTake = 10;

            if (IsAtLastLevel())
                hitsToTake = 3;

            return predictedDamageToSelf * hitsToTake < Health;
        }

        public bool CanMakeStraightSafeRunTo(Location to, ref Route safeStraightRoute)
        {
            return Location.IsOnStraightSafeRun(to, this, ref safeStraightRoute);
        }

        private class StateIdle : State<PlayerBot>
        {
            public StateIdle(PlayerBot self)
                : base(self)
            {
            }

            public override void Tick()
            {
                var health = default(HealthPackView);
                if (Self.CanGrabHealth(ref health) && Self.Health < HealthMaximum)
                {
                    var healthStep = health.Location - Self.Location;
                    Self.NextTurn = Turn.Step(healthStep);

                    return;
                }

                if (Self.TrySetTargetMonster())
                {
                    GoToState(() => new StateSeeMonster(Self));
                    return;
                }

                GoToState(() => new StateExitLevel(Self));
            }

            public override void GoToState<TState>(Func<TState> factory)
            {
                Self.State = factory();
                Self.State.Tick();
            }
        }

        private class StateSeeMonster : State<PlayerBot>
        {
            public StateSeeMonster(PlayerBot self)
                : base(self)
            {
            }

            public override void Tick()
            {
                var escapeStep = default(Offset);
                if (Self.TryAvoidCornering(ref escapeStep))
                {
                    Self.NextTurn = Turn.Step(escapeStep);
                    return;
                }

                var attackRoute = BreadthFirstSearchAlgorithm.GetRouteTo(Self, Self.TargetMonster.Location);
                if (attackRoute.Count == 0)
                {
                    GoToState(() => new StateExitLevel(Self));
                    return;
                }

                var crowd = Self.LevelView.Monsters.GetMonstersAround(attackRoute.Last());
                if (crowd.Count() > 1)
                {
                    GoToState(() => new StateCrowdFighter(Self, crowd, attackRoute));
                    return;
                }

                if (Self.IsFitnessedToFightWith(Self.TargetMonster))
                {
                    if (Self.IsInAttackRange(Self.TargetMonster))
                    {
                        var attackOffset = Self.TargetMonster.Location - Self.Location;
                        Self.NextTurn = Turn.Attack(attackOffset);

                        return;
                    }

                    var item = default(ItemView);
                    if (Self.CanGrabItem(ref item))

                        if (!Self.EquippedItem.HasValue ||
                            item.GetItemValue() > Self.EquippedItem.GetItemValue())
                        {
                            var itemStep = item.Location - Self.Location;

                            Self.NextTurn = Turn.Step(itemStep);
                            return;
                        }

                    var attackStep = (attackRoute.First() - Self.Location).SnapToStep();
                    Self.NextTurn = Turn.Step(attackStep);

                    return;
                }
                
                GoToState(() => new StateExitLevel(Self));
            }

            public override void GoToState<TState>(Func<TState> factory)
            {
                Self.State = factory();
                Self.State.Tick();
            }
        }

        private class StateCrowdFighter : State<PlayerBot>
        {
            private readonly IEnumerable<PawnView> crowd;
            private readonly Route attackRoute;
            public StateCrowdFighter(PlayerBot self, IEnumerable<PawnView> crowd, Route attackRoute)
                : base(self)
            {
                this.crowd = crowd;
                this.attackRoute = attackRoute;
            }

            public override void Tick()
            {
                if (Self.IsFitnessedToFightWith(crowd))
                {
                    if (Self.IsInAttackRange(Self.TargetMonster))
                    {
                        var attackOffset = Self.TargetMonster.Location - Self.Location;
                        Self.NextTurn = Turn.Attack(attackOffset);

                        return;
                    }

                    var attackStep = (attackRoute.First() - Self.Location).SnapToStep();
                    Self.NextTurn = Turn.Step(attackStep);

                    return;
                }

                GoToState(() => new StateExitLevel(Self));
            }

            public override void GoToState<TState>(Func<TState> factory)
            {
                Self.State = factory();
                Self.State.Tick();
            }
        }

        private class StateSeeObjective : State<PlayerBot>
        {
            private readonly Route objectiveRoute;
            public StateSeeObjective(PlayerBot self, Route objectiveRoute)
                : base(self)
            {
                this.objectiveRoute = objectiveRoute;
            }

            public override void Tick()
            {
                var objectiveOffset = default(Offset);
                if (Self.Objective != null)
                    objectiveOffset = Self.Objective is HealthPackView
                        ? ((HealthPackView)Self.Objective).Location - Self.Location
                        : ((ItemView)Self.Objective).Location - Self.Location;

                var objectiveStep = objectiveOffset.SnapToStep();
                if (objectiveRoute.Count == 0)
                    if (Self.LevelView.Monsters.Where(Self.IsInAttackRange).Any())
                    {
                        var attackOffset = Self.LevelView.Monsters
                            .Where(Self.IsInAttackRange)
                            .OrderBy(m => m.Health)
                            .First().Location - Self.Location;
                        Self.NextTurn = Turn.Attack(attackOffset);

                        return;
                    }
                    else return;

                objectiveStep = (objectiveRoute.First() - Self.Location).SnapToStep();
                Self.NextTurn = Turn.Step(objectiveStep);
            }

            public override void GoToState<TState>(Func<TState> factory)
            {
                Self.State = factory();
                Self.State.Tick();
            }
        }

        private class StateExitLevel : State<PlayerBot>
        {
            public StateExitLevel(PlayerBot self) : base(self)
            {
            }

            public override void Tick()
            {
                var objectiveRoute = default(Route);
                if (Self.TrySetObjective(needHealth: Self.Health < HealthMaximum, needItem: Self.Health == HealthMaximum, objectiveRoute: ref objectiveRoute))

                    if (Self.Objective != null)
                    {
                        GoToState(() => new StateSeeObjective(Self, objectiveRoute));
                        return;
                    }

                var exitLocation = Self.LevelView.Field.GetCellsOfType(CellType.Exit).First();
                var exitRoute = BreadthFirstSearchAlgorithm.GetRouteTo(Self, exitLocation);

                if (exitRoute.Count == 0)
                    if (Self.LevelView.Monsters.Where(Self.IsInAttackRange).Any())
                    {
                        Self.NextTurn = Turn.Attack(Self.LevelView.Monsters.First(Self.IsInAttackRange).Location - Self.Location);
                        return;
                    }
                    else if (Self.LevelView.Monsters.Any())
                    {
                        var stepAttack = Self.LevelView.Monsters
                            .OrderBy(m => Self.Location.GetDistanceTo(m.Location))
                            .ThenBy(m => m.Health)
                            .First().Location - Self.Location;
                        Self.NextTurn = Turn.Step(stepAttack.SnapToStep());

                        return;
                    }
                    else return;

                var exitStep = (exitRoute.First() - Self.Location).SnapToStep();
                if (Self.Location + exitStep == exitLocation)
                    Self.TargetMonster = default(PawnView);

                Self.NextTurn = Turn.Step(exitStep);
            }

            public override void GoToState<TState>(Func<TState> factory)
            {
                Self.State = factory();
                Self.State.Tick();
            }
        }
    }
}