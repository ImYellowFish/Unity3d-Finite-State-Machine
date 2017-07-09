using System;
using System.Collections;
using System.Collections.Generic;

namespace MonsterLove.StateMachine
{   
    public interface ITransitionManager {
        IStateConfiguration[] ConfigurationList { get; }
    }


    public interface IStateConfiguration
    {
        ITransition[] Transitions { get; }
    }



    public interface ITransition
    {
        string FromStateName { get; }
        string ToStateName { get; }
        string TriggerName { get; }
        bool HasGuard { get; }
        StateTransition overrideSetting { get; }
    }



    /// <summary>
    /// Manage state transitions
    /// </summary>
    public class TransitionManager<TTrigger, TState> : ITransitionManager
        where TState: struct, IConvertible, IComparable 
        where TTrigger : struct, IConvertible, IComparable
    {
        public event Action<Transition<TTrigger, TState>> Triggered;
        public Array states;

        public TransitionManager(StateMachine<TState> fsm)
        {
            this.fsm = fsm;

            states = Enum.GetValues(typeof(TState));
        }

        public StateConfiguration<TTrigger, TState> Configure(TState state)
        {
            //dirty = true;

            if (dict.ContainsKey(state))
            {
                return dict[state];
            }
            else
            {
                var stateConfig = new StateConfiguration<TTrigger, TState>(this, state);
                dict.Add(state, stateConfig);
                return stateConfig;
            }
        }
        
        /// <summary>
        /// fires some trigger
        /// changes the state if needed
        /// 
        /// returns true if a valid transition is called
        /// 
        /// </summary>
        /// <param name="trigger"></param>
        public bool Fire(TTrigger trigger)
        {
            try
            {
                object fromState = fsm.CurrentStateMap.state;
                Transition<TTrigger, TState> transition = dict[fromState].ProcessTrigger(trigger);

                fsm.ChangeState(transition.toState, transition.overrideSetting);

                if(Triggered != null)
                    Triggered.Invoke(transition);

                return true;
            }
            catch
            {
                // does not find valid transition regarding this state
                return false;
            }
        }


        public void Clear()
        {
            //dirty = true;

            dict.Clear();
        }


        /// <summary>
        /// Permit all state to transition to toState by a specified trigger
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="toState"></param>
        /// <param name="overrideSetting"></param>
        public TransitionManager<TTrigger, TState> PermitAll(TTrigger trigger,
            TState toState,
            StateTransition overrideSetting = StateTransition.Safe)
        {
            foreach (var state in states)
            {
                try
                {
                    Configure((TState)state).Permit(trigger, toState, overrideSetting);
                }
                catch (ArgumentException)
                {
                    // This means the state cannot transition to itself
                }
            }
            return this;
        }

        /// <summary>
        /// Permit all state to transition to toState by a specified trigger, with guard
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="toState"></param>
        /// <param name="overrideSetting"></param>
        public TransitionManager<TTrigger, TState> PermitAll(TTrigger trigger,
            TState toState,
             Func<bool> guard,
            StateTransition overrideSetting = StateTransition.Safe)
        {
            foreach (var state in states)
            {
                try
                {
                    Configure((TState)state).PermitIf(trigger, toState, guard, overrideSetting);
                }
                catch (ArgumentException)
                {
                    // This means the state cannot transition to itself
                }
            }
            return this;
        }

        /// <summary>
        /// Remove a transition with specified fromState and trigger
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public TransitionManager<TTrigger, TState> Remove(TState fromState, TTrigger trigger)
        {
            Configure(fromState).Remove(trigger);

            return this;
        }


        public IStateConfiguration[] ConfigurationList {
            get {
                StateConfiguration<TTrigger, TState>[] result = new StateConfiguration<TTrigger, TState>[dict.Count];
                dict.Values.CopyTo(result, 0);
                return result;
            }
        }

        public StateMachine<TState> fsm;

        public TState CurrentState
        {
            get
            {
                object state = fsm.CurrentStateMap.state;
                if (state != null)
                    return (TState)state;
                return (TState)states.GetValue(0);
            }
        }

        private Dictionary<object, StateConfiguration<TTrigger, TState>> dict = new Dictionary<object, StateConfiguration<TTrigger, TState>>();

    }


    /// <summary>
    /// stores a list of transitions starting from one state
    /// </summary>
    /// <typeparam name="TTrigger"></typeparam>
    /// <typeparam name="TState"></typeparam>
    public class StateConfiguration<TTrigger, TState> : IStateConfiguration
        where TState : struct, IConvertible, IComparable
        where TTrigger : struct, IConvertible, IComparable 
    {

        private TState state;        
        private List<Transition<TTrigger, TState>> transitions { get; set; }
        private const string NO_GUARD_DESCRIPTION = "NO_GUARD";
        private StateMachine<TState> fsm;
        

        public StateConfiguration(TransitionManager<TTrigger, TState> transitionManager, TState state)
        {
            this.state = state;
            transitions = new List<Transition<TTrigger, TState>>();
            fsm = transitionManager.fsm;
        }



        private void EnforceNotIdentityTransition(TState toState)
        {
            if (state.Equals(toState) && !fsm.enableTransitionToSelf)
            {
                throw new ArgumentException("The FSM Cannot transit to self state: " + toState.ToString());
            }
        }

        /// <summary>
        /// adds a transition regarding a specified trigger, without guard
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="toState"></param>
        /// <returns></returns>
        public StateConfiguration<TTrigger, TState> Permit(TTrigger trigger, TState toState,
            StateTransition overrideSetting = StateTransition.Safe)
        {
            EnforceNotIdentityTransition(toState);
            transitions.Add(new Transition<TTrigger, TState>(
                trigger,
                state,
                toState,
                overrideSetting
            ));
            return this;

        }

        /// <summary>
        /// adds a transition with guard
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="toState"></param>
        /// <param name="guard"></param>
        /// <returns></returns>
        public StateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, 
            TState toState, 
            Func<bool> guard,
            StateTransition overrideSetting = StateTransition.Safe)
        {
            EnforceNotIdentityTransition(toState);
            transitions.Add(new Transition<TTrigger, TState>(
                trigger,
                state,
                toState,
                guard,
                overrideSetting
            ));
            return this;
        }

        /// <summary>
        /// Remove a transition with the specified trigger
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public StateConfiguration<TTrigger, TState> Remove(TTrigger trigger)
        {
            var transition = transitions.Find((t) => t.trigger.Equals(trigger));
            if (transition != null)
                transitions.Remove(transition);

            return this;
        }
        

        /// <summary>
        /// checks whether the state should change when some trigger is fired
        /// returns the destination state
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public Transition<TTrigger, TState> ProcessTrigger(TTrigger trigger)
        {
            Transition<TTrigger, TState> transition = transitions.Find((t) => {
                return t.trigger.Equals(trigger) && t.guard.Invoke();
            });

            return transition;
        }

        
        public ITransition[] Transitions {
            get {
                return transitions.ToArray();
            }
        }
    }


    /// <summary>
    /// stores a single transition, from stateA to stateB, with guard if necessary
    /// </summary>
    public class Transition<TTrigger, TState> : ITransition
        where TState : struct, IConvertible, IComparable
        where TTrigger : struct, IConvertible, IComparable
    {
        public TState fromState;
        public TState toState;
        public TTrigger trigger;
        public Func<bool> guard;

        public Transition(TTrigger trigger, TState fromState, TState toState, StateTransition overrideSetting) {
            this.fromState = fromState;
            this.toState = toState;
            this.trigger = trigger;
            this.guard = AlwaysTrueFunc;
            this.overrideSetting = overrideSetting;
        }

        public Transition(TTrigger trigger, TState fromState, TState toState, Func<bool> guard, StateTransition overrideSetting) {
            this.fromState = fromState;
            this.toState = toState;
            this.trigger = trigger;
            this.guard = guard;
            this.overrideSetting = overrideSetting;
        }

        public string FromStateName { get { return fromState.ToString(); } }
        public string ToStateName { get { return toState.ToString(); } }
        public string TriggerName { get { return trigger.ToString(); } }
        public bool HasGuard { get { return guard != AlwaysTrueFunc; } }
        public StateTransition overrideSetting { get; set; }

        private static bool AlwaysTrueFunc() {
            return true;
        }
        
    }
}