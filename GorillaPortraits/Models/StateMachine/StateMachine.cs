using System;

namespace GorillaPortraits.Models.StateMachine
{
    public class StateMachine<T> where T : State
    {
        public T CurrentState => currentState;
        public bool HasState => currentState is not null;
        public event Action<T> OnStateChanged;

        protected T currentState;

        public void SwitchState(T newState)
        {
            if (HasState)
                currentState.Exit();

            OnStateChanged?.Invoke(newState);
            currentState = newState;
            currentState?.Enter();
        }

        public void Update()
        {
            if (HasState)
                currentState.Update();
        }
    }
}
