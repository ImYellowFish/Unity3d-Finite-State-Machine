using UnityEngine;
using System.Collections;
using MonsterLove.StateMachine;
using UnityEditor;

public class MainForTransition : MonoBehaviour {
    TestFSMDebugger[] targets;


    int index = 0;

    private void Start()
    {
        targets = FindObjectsOfType<TestFSMDebugger>();
    }

    
    void OnGUI()
    {
        if (targets == null || targets.Length != 2)
            return;

        if (to_triggerNames == null)
            InitTransitionNames(targets[index].GetComponent<StateMachineDebugger>());

        GUILayout.Label("Current Focus: " + targets[index].gameObject.name);

        if (GUILayout.Button("Change Focus", GUILayout.Width(150)))
        {
            index = 1 - index;
        }
        GUILayout.Space(30);

        var smd = targets[index].GetComponent<StateMachineDebugger>();
        GUILayout.BeginVertical();
        if (smd.transitionValid)
        {
            for(int i = 0; i < to_triggerNames.Length; i++)
            {
                if(GUILayout.Button("Invoke Trigger: " + to_triggerNames[i], GUILayout.Width(250)))
                    smd.InvokeTrigger(smd.triggers.GetValue(to_triggerValues[i]));
            }
        }
        GUILayout.EndVertical();

        GUILayout.Space(30);
        GUILayout.TextArea("To check state history info, select cube or sphere in Hierachy, and then check StateMachineDebugger component in inspector", 
            GUILayout.Width(350));
    }

    private int[] to_triggerValues;
    private string[] to_triggerNames;

    private void InitTransitionNames(StateMachineDebugger smd)
    {
        if (!smd.transitionValid)
            return;

        to_triggerNames = new string[smd.triggers.Length];
        to_triggerValues = new int[smd.triggers.Length];

        for (int i = 0; i < to_triggerValues.Length; i++)
        {
            to_triggerValues[i] = i;
            to_triggerNames[i] = smd.triggers.GetValue(i).ToString();
        }
    }
}
