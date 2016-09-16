using System;

namespace SpurRoguelike.PlayerBot
{
    internal abstract class State<T> 
    {
        protected State(T self)
        {
            Self = self;
        }

        public abstract void Tick();

        public abstract void GoToState<TState>(Func<TState> factory) where TState : State<T>; 

        protected T Self;
    }
}