using UnityEngine;
using System.Collections;
using MonsterLove.StateMachine;

public class TestFSMDebugger : MonoBehaviour {
    public enum State { A = 0, B = 1, NotAffectedByGlobal = 2}
    private StateMachine<State> fsm;
    public enum Trigger { AtoB = 0, BtoA = 1, AlltoA = 4, AlltoB = 2, NotUsed = 3}
    TransitionManager<Trigger, State> trm;

    public bool enableTransitionToSelfState = true;

    // Use this for initialization
    void Start () {
        fsm = StateMachine<State>.Initialize(this, State.A, enableTransitionToSelfState);
        trm = new TransitionManager<Trigger, State>(fsm);
        StateMachineDebugger.Create(gameObject, fsm, trm);

        trm.Configure(State.A).Permit(Trigger.AtoB, State.B);
        trm.Configure(State.B).Permit(Trigger.BtoA, State.A);
        trm.PermitAll(Trigger.AlltoA, State.A);
        trm.PermitAll(Trigger.AlltoB, State.B, StateTransition.Overwrite).
            Remove(State.NotAffectedByGlobal, Trigger.AlltoA).
            Remove(State.NotAffectedByGlobal, Trigger.AlltoB);
        

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void A_Enter()
    {
        Debug.Log("A_ENTER");
        GetComponent<Renderer>().material.color = Color.white;
    }

    IEnumerator B_Enter()
    {
        Debug.Log("B_ENTER");
        GetComponent<Renderer>().material.color = Color.yellow;
        yield return new WaitForSeconds(1);
        Debug.Log("B_ENTER_END");
        GetComponent<Renderer>().material.color = Color.blue;

    }

    [ContextMenu("Fire Not Used Trigger")]
    public void FireNotUsedTrigger()
    {
        bool val = trm.Fire(Trigger.NotUsed);
        Debug.Log("Trigger accepted: " + val);
    }

    [ContextMenu("Fire All to A Trigger")]
    public void FireAlltoATrigger()
    {
        bool val = trm.Fire(Trigger.AlltoA);
        Debug.Log("Trigger accepted: " + val);
    }
}
