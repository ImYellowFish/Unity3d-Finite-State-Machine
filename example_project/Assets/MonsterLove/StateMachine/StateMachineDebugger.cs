using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace MonsterLove.StateMachine {
    public class StateMachineDebugger : MonoBehaviour {
        
        public IStateMachine stateMachine { get; private set; }
        public ITransitionManager transitionManager { get; private set; }
        public Array states { get; private set; }
        public Array triggers { get; private set;}

        public bool stateValid { get { return stateMachine != null && states != null && stateMachine.CurrentStateMap.state != null; } }
        public bool transitionValid { get { return transitionManager != null && triggers != null; } }

        public string currentStateName;

        [HideInInspector]
        public bool timeAscending = false;

        public ITransition previousActiveTransition { get; private set; }

        [System.Serializable]
        public struct TransitionRecord
        {
            public int index;
            public float time;
            public GameObject gameObject;
            public System.Diagnostics.StackTrace stackTrace;
            public ITransition transition;
        }

        [HideInInspector]
        public int transitionHistorySize = 30;
        
        [HideInInspector]
        public List<TransitionRecord> transitionHistory = new List<TransitionRecord>();
        public static int g_nextTransitionIndex = 0;

        public static int g_transitionHistorySize = 200;
        public static List<TransitionRecord> g_transitionHistory = new List<TransitionRecord>();


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

        public static StateMachineDebugger Create<TState>(GameObject gameObject, 
            StateMachine<TState> stateMachine)
            where TState : struct, IConvertible, IComparable
        {
            var debugger = gameObject.AddComponent<StateMachineDebugger>();
            debugger.Register(stateMachine);
            return debugger;
        }

        public static StateMachineDebugger Create<TTrigger, TState>(GameObject gameObject,
            StateMachine<TState> stateMachine,
            TransitionManager<TTrigger, TState> transitionManager)
            where TState : struct, IConvertible, IComparable
            where TTrigger : struct, IConvertible, IComparable
        {
            var debugger = gameObject.AddComponent<StateMachineDebugger>();
            debugger.Register(stateMachine, transitionManager);
            return debugger;
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
            states = transitionManager.states;
            changeToStateAction = s => stateMachine.ChangeState((TState)s);

            if (transitionManager != null)
            {
                triggers = Enum.GetValues(typeof(TTrigger));
                invokeTriggerAction = t => transitionManager.Fire((TTrigger)t);

                transitionManager.BeforeTriggered += (t => previousActiveTransition = t);
                transitionManager.BeforeTriggered += AddTransitionRecord;
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

        private void AddTransitionRecord(ITransition transitionRecord)
        {
            TransitionRecord record = new TransitionRecord();
            record.transition = transitionRecord;
            record.time = Time.time;
            record.gameObject = stateMachine.Component.gameObject;
            record.stackTrace = new System.Diagnostics.StackTrace(true);
            record.index = g_nextTransitionIndex;
            g_nextTransitionIndex++;

            
            transitionHistory.Add(record);
            if (transitionHistory.Count > transitionHistorySize)
            {
                transitionHistory.RemoveAt(0);

            }

            // add to global history
            g_transitionHistory.Add(record);
            if (g_transitionHistory.Count > g_transitionHistorySize)
            {
                g_transitionHistory.RemoveAt(0);
            }
        }

        #region Test
        public enum States { idle, walk, run, fight, dead }
        public enum Triggers { startWalk, stop, startRun }

        [ContextMenu("Test Get String")]
        public void TestGetString() {
            var fsm = StateMachine<States>.Initialize(this);
            fsm.ChangeState(States.idle);

            var tm = new TransitionManager<Triggers, States>(fsm);
            tm.Configure(States.idle).Permit(Triggers.startWalk, States.walk).
                Permit(Triggers.startRun, States.run);
            tm.Configure(States.walk).Permit(Triggers.stop, States.idle);
            tm.Configure(States.run).Permit(Triggers.stop, States.idle);
            tm.Configure(States.fight).PermitIf(Triggers.stop, States.idle, () => true);

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