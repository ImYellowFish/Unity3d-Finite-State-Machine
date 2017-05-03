using System;
using System.Collections;
using System.Collections.Generic;

namespace MonsterLove.StateMachine
{   
    public interface ITransitionManager {
        IStateConfiguration[] ConfigurationList { get; }
    }

    /// <summary>
    /// Manage state transitions
    /// </summary>
    public class TransitionManager<TTrigger, TState> : ITransitionManager
        where TState: struct, IConvertible, IComparable 
        where TTrigger : struct, IConvertible, IComparable
    {
        public event Action<Transition<TTrigger, TState>> Triggered;

        public TransitionManager(StateMachine<TState> fsm)
        {
            this.fsm = fsm;
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
        /// </summary>
        /// <param name="trigger"></param>
        public void Fire(TTrigger trigger)
        {
            try
            {
                object fromState = fsm.CurrentStateMap.state;
                Transition<TTrigger, TState> transition = dict[fromState].ProcessTrigger(trigger);
                if (!transition.toState.Equals(fromState))
                {
                    fsm.ChangeState(transition.toState);

                    if(Triggered != null)
                        Triggered.Invoke(transition);
                }
            }
            catch
            {
                // does not find valid transition regarding this state
            }
        }


        public void Clear()
        {
            //dirty = true;

            dict.Clear();
        }


        public IStateConfiguration[] ConfigurationList {
            get {
                StateConfiguration<TTrigger, TState>[] result = new StateConfiguration<TTrigger, TState>[dict.Count];
                dict.Values.CopyTo(result, 0);
                return result;
            }
        }

        private StateMachine<TState> fsm;
        private Dictionary<object, StateConfiguration<TTrigger, TState>> dict = new Dictionary<object, StateConfiguration<TTrigger, TState>>();

    }

    public interface IStateConfiguration {
        ITransition[] Transitions { get; }
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

        public StateConfiguration(TransitionManager<TTrigger, TState> fsmConfig, TState state)
        {
            this.state = state;
            transitions = new List<Transition<TTrigger, TState>>();
        }

        /// <summary>
        /// adds a transition regarding a specified trigger, without guard
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="toState"></param>
        /// <returns></returns>
        public StateConfiguration<TTrigger, TState> Permit(TTrigger trigger, TState toState)
        {
            EnforceNotIdentityTransition(toState);
            transitions.Add(new Transition<TTrigger, TState>(
                trigger,
                state,
                toState
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
        public StateConfiguration<TTrigger, TState> PermitIf(TTrigger trigger, TState toState, Func<bool> guard)
        {
            EnforceNotIdentityTransition(toState);
            transitions.Add(new Transition<TTrigger, TState>(
                trigger,
                state,
                toState,
                guard
            ));
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


        private void EnforceNotIdentityTransition(TState toState)
        {
            if(state.Equals(toState))
            {
                throw new ArgumentException("Cannot transit to self state: " + toState.ToString());
            }
        }

        

        public ITransition[] Transitions {
            get {
                return transitions.ToArray();
            }
        }
    }


    public interface ITransition {
        string FromStateName { get; }
        string ToStateName { get; }
        string TriggerName { get; }
        bool HasGuard { get; }
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

        public Transition(TTrigger trigger, TState fromState, TState toState) {
            this.fromState = fromState;
            this.toState = toState;
            this.trigger = trigger;
            this.guard = AlwaysTrueFunc;
        }

        public Transition(TTrigger trigger, TState fromState, TState toState, Func<bool> guard) {
            this.fromState = fromState;
            this.toState = toState;
            this.trigger = trigger;
            this.guard = guard;
        }

        public string FromStateName { get { return fromState.ToString(); } }
        public string ToStateName { get { return toState.ToString(); } }
        public string TriggerName { get { return trigger.ToString(); } }
        public bool HasGuard { get { return guard != AlwaysTrueFunc; } }


        private static bool AlwaysTrueFunc() {
            return true;
        }
        
    }
}