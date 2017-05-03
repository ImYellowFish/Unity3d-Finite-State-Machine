using UnityEngine;
using System;
using System.Reflection;
using System.Collections;

namespace MonsterLove.StateMachine {
    public class StateMachineDebugger : MonoBehaviour {
        
        public IStateMachine stateMachine { get; private set; }
        public ITransitionManager transitionManager { get; private set; }
        public Array states { get; private set; }
        public Array triggers { get; private set;}

        public bool stateValid { get { return stateMachine != null && states != null; } }
        public bool transitionValid { get { return transitionManager != null && triggers != null; } }

        public string currentStateName { get; private set; }
        public ITransition previousActiveTransition { get; private set; }

        public void InvokeChangeState(object state)
        {
            if(changeToStateAction != null)
                changeToStateAction.Invoke(state);
        }

        public void InvokeTrigger(object trigger)
        {
            if (invokeTriggerAction != null)
                invokeTriggerAction(trigger);
        }

        public void Register<TState>(StateMachine<TState> stateMachine)
            where TState : struct, IConvertible, IComparable
        {
            Register<TState, TState>(stateMachine, null);
        }

        public void Register<TTrigger, TState>(StateMachine<TState> stateMachine, TransitionManager<TTrigger, TState> transitionManager)
            where TState: struct, IConvertible, IComparable
            where TTrigger : struct, IConvertible, IComparable
        {
            this.stateMachine = stateMachine;
            this.transitionManager = transitionManager;
            states = Enum.GetValues(typeof(TState));
            changeToStateAction = s => stateMachine.ChangeState((TState)s);

            if (transitionManager != null)
            {
                triggers = Enum.GetValues(typeof(TTrigger));
                invokeTriggerAction = t => transitionManager.Fire((TTrigger)t);

                transitionManager.Triggered += (t => previousActiveTransition = t);
            }
        }

        
        private Action<object> changeToStateAction;
        private Action<object> invokeTriggerAction;

        private void Update()
        {
            if (stateValid)
            {
                currentStateName = stateMachine.CurrentStateMap.state.ToString();
            }
        }

        #region Test
        public enum States { idle, walk, run, fight, dead }
        public enum Triggers { startWalk, stop, startRun }
        public bool testVal;
        [ContextMenu("Test Get String")]
        public void TestGetString() {
            var fsm = StateMachine<States>.Initialize(this);
            fsm.ChangeState(States.idle);

            var tm = new TransitionManager<Triggers, States>(fsm);
            tm.Configure(States.idle).Permit(Triggers.startWalk, States.walk).
                Permit(Triggers.startRun, States.run);
            tm.Configure(States.walk).Permit(Triggers.stop, States.idle);
            tm.Configure(States.run).Permit(Triggers.stop, States.idle);
            tm.Configure(States.fight).PermitIf(Triggers.stop, States.idle, () => testVal);

            Register(fsm, tm);
            
            Debug.Log("States: ");
            foreach(object s in states) {
                Debug.Log(s.ToString());
            }

            Debug.Log("Transitions:");
            foreach(IStateConfiguration sc in transitionManager.ConfigurationList) {
                foreach(ITransition t in sc.Transitions) {
                    Debug.Log("from: " + t.FromStateName);
                    Debug.Log("to: " + t.ToStateName);
                    Debug.Log("trigger: " + t.TriggerName);
                    Debug.Log("has guard: " + t.HasGuard);
                }
            }

            testFsm = fsm;
        }

        private StateMachine<States> testFsm;
        private int TestCurrentStateIndex = 0;
        
        [ContextMenu("Test Next State")]
        public void TestNextState()
        {
            TestCurrentStateIndex++;
            TestCurrentStateIndex = TestCurrentStateIndex % states.Length;
            testFsm.ChangeState((States)TestCurrentStateIndex);
        }

        [ContextMenu("Test without transitions")]
        public void TestWithoutTransitions()
        {
            var fsm = StateMachine<States>.Initialize(this);
            fsm.ChangeState(States.idle);
            
            Register(fsm);
        }
        #endregion
    }
}