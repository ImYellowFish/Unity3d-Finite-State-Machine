using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MonsterLove.StateMachine {
    [CustomEditor(typeof(StateMachineDebugger))]
    public class StateMachineDebuggerEditor : Editor {
        private const float STATE_WIDTH = 100;
        private const float STATE_HEIGHT = 50;

        public void OnEnable() {

        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            
            StateMachineDebugger smd = target as StateMachineDebugger;
            if (!smd.stateValid)
                return;

            DrawOptions(smd);
            DrawStates(smd);
            DrawTransitions(smd);
        }

        public class StateInfo {
            public string stateName;
            public int index;
            public Rect stateRect;
            public Color color;
        }

        // options
        private int changeToState;
        private int[] to_stateValues;
        private string[] to_stateNames;

        private int invokeTrigger;
        private int[] to_triggerValues;
        private string[] to_triggerNames;

        private int showTransitionOfState;
        private int[] show_statePopupValues;
        private string[] show_statePopupNames;


        private void DrawOptions(StateMachineDebugger smd)
        {
            if (!smd.stateValid)
            {
                return;
            }

            if (to_stateValues == null || to_stateValues.Length < smd.states.Length)
            {
                InitStateNames(smd);
            }

            if(to_triggerValues == null || to_triggerValues.Length < smd.triggers.Length)
            {
                InitTransitionNames(smd);
            }
           
            showTransitionOfState = EditorGUILayout.IntPopup("Show Transition", showTransitionOfState, show_statePopupNames, show_statePopupValues);
            EditorGUILayout.Space();

            changeToState = EditorGUILayout.IntPopup("Change to target state", changeToState, to_stateNames, to_stateValues);
            if(GUILayout.Button("Change State"))
            {
                smd.InvokeChangeState(smd.states.GetValue(changeToState));
            }
            

            if (smd.transitionValid)
            {
                EditorGUILayout.Space();
                invokeTrigger = EditorGUILayout.IntPopup("Invoke Trigger", invokeTrigger, to_triggerNames, to_triggerValues);
                if (GUILayout.Button("Invoke Trigger"))
                {
                    smd.InvokeTrigger(smd.triggers.GetValue(invokeTrigger));
                }
            }
        }

        Dictionary<string, StateInfo> dict = new Dictionary<string, StateInfo>();
        private GUIStyle style;

        private void DrawStates(StateMachineDebugger smd) {
            if (!smd.stateValid)
                return;

            int stateCount = smd.states.Length;
            style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;

            for(int i = 0; i < stateCount; i++) {
                Rect stateRect = GUILayoutUtility.GetRect (STATE_WIDTH, STATE_HEIGHT);
                stateRect.width = STATE_WIDTH;
                
                
                string stateName = smd.states.GetValue(i).ToString();
                Color stateColor;
                if (!dict.ContainsKey(stateName))
                {
                    stateColor = GetPresetColor(i);
                }else
                {
                    stateColor = dict[stateName].color;
                }

                style.normal.textColor = stateColor;
                if (stateName == smd.currentStateName)
                {
                    Color hlColor = Color.yellow;
                    EditorGUI.DrawRect(stateRect, hlColor);
                }

                Color bgColor = stateColor * 0.4f;
                bgColor.a = 1;
                EditorGUI.DrawRect(ScaleRect(stateRect, 0.9f), bgColor);

                EditorGUI.LabelField(ScaleRect(stateRect, 0.9f), stateName, style);
                // EditorGUILayout.TextField(ScaleRect(stateRect, 0.9f), stateName);

                // TODO refresh
                if (!dict.ContainsKey(stateName)) {
                    dict.Add(stateName, new StateInfo {
                        stateName = stateName,
                        index = i,
                        stateRect = new Rect(stateRect),
                        color = stateColor,
                    });
                } else {
                    dict[stateName].stateRect = stateRect;
                }
                // smd.states.GetValue(i).ToString();
            }
        }

        private void DrawTransitions(StateMachineDebugger smd)
        {
            if (!smd.transitionValid)
                return;

            var scList = smd.transitionManager.ConfigurationList;
            foreach (IStateConfiguration sc in scList)
            {
                var transitionList = sc.Transitions;
                StateInfo stateInfo = dict[transitionList[0].FromStateName];
                if (!ShouldShowTransitionOfIndex(stateInfo.index))
                    continue;
                
                Vector2 verticalLineStartPos = new Vector2(stateInfo.stateRect.xMax +
                    (stateInfo.index + 1) * STATE_WIDTH / 2 + STATE_WIDTH, stateInfo.stateRect.center.y - STATE_HEIGHT / 4);
                Vector2 horizontalLineStartPos = new Vector2(stateInfo.stateRect.xMax, verticalLineStartPos.y);

                Handles.color = stateInfo.color;

                
                // draw transition lines
                foreach (ITransition t in transitionList)
                {
                    StateInfo toStateInfo = dict[t.ToStateName];
                    Vector2 verticalLineEndPos = new Vector2(verticalLineStartPos.x, toStateInfo.stateRect.center.y + (stateInfo.index * 4));
                    Vector2 horizontalLineEndPos = new Vector2(toStateInfo.stateRect.xMax, verticalLineEndPos.y);

                    if (OnlyShowLastActiveTransition && t != smd.previousActiveTransition)
                    {
                        continue;
                    }

                    if (t == smd.previousActiveTransition)
                    {
                        Handles.color = Color.yellow;
                        Handles.DrawAAPolyLine(10, horizontalLineStartPos, verticalLineStartPos, verticalLineEndPos);
                        Handles.DrawAAPolyLine(5, verticalLineEndPos, horizontalLineEndPos);
                        
                        Handles.color = stateInfo.color;
                    }
                    Handles.DrawAAPolyLine(5, horizontalLineStartPos, verticalLineStartPos, verticalLineEndPos);
                    Handles.DrawLine(verticalLineEndPos, horizontalLineEndPos);
                    DrawArrowCap(verticalLineEndPos + Vector2.left * 5, Vector3.left, 5);
                    DrawArrowCap(horizontalLineEndPos, Vector3.left, 5);

                }
                Handles.EndGUI();


                // draw transition labels
                foreach (ITransition t in transitionList)
                {
                    StateInfo toStateInfo = dict[t.ToStateName];
                    Vector2 verticalLineEndPos = new Vector2(verticalLineStartPos.x, toStateInfo.stateRect.center.y + (stateInfo.index * 4));

                    if (OnlyShowLastActiveTransition && t != smd.previousActiveTransition)
                    {
                        continue;
                    }

                    Vector2 labelSize = new Vector2(80, 15);
                    Vector2 labelCenterPos = verticalLineEndPos - Vector2.right * labelSize.x / 2;
                    EditorGUI.DrawRect(new Rect(labelCenterPos, labelSize), Color.white * 0.3f);
                    style.normal.textColor = Color.white;

                    
                    string triggerName = t.TriggerName;
                    if (t.HasGuard)
                    {
                        triggerName += "(G)";
                    }
                    EditorGUI.LabelField(new Rect(labelCenterPos, labelSize), triggerName, style);
                }
            }

        }

        private static Rect ScaleRect(Rect original, float scale) {
            Vector2 center = original.center;
            Rect result = new Rect(original.position, original.size * scale);
            result.center = center;
            return result;
        }
        
        private static void DrawArrowCap(Vector3 pointerPos, Vector3 direction, float size)
        {
            Handles.DrawAAConvexPolygon(pointerPos, 
                pointerPos + size * Vector3.up - size * direction, 
                pointerPos - size * Vector3.up - size * direction);
        }

        private static readonly Color[] presetColors = new Color[]
        {
            Color.blue + Color.yellow, Color.red, Color.magenta, Color.green, Color.cyan
        };

        private static Color GetRandomColor()
        {
            return Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        }

        private static Color GetPresetColor(int index)
        {
            return presetColors[index % presetColors.Length];
        }

        private void InitStateNames(StateMachineDebugger smd)
        {
            if (!smd.stateValid)
                return;

            to_stateValues = new int[smd.states.Length];
            to_stateNames = new string[smd.states.Length];

            show_statePopupValues = new int[smd.states.Length + 2];
            show_statePopupNames = new string[smd.states.Length + 2];

            for (int i = 0; i < to_stateValues.Length; i++)
            {
                to_stateValues[i] = i;
                to_stateNames[i] = smd.states.GetValue(i).ToString();
            }


            show_statePopupValues[smd.states.Length] = smd.states.Length;
            show_statePopupValues[smd.states.Length + 1] = smd.states.Length + 1;
            show_statePopupNames[0] = "all";
            show_statePopupNames[1] = "last active";

            to_stateNames.CopyTo(show_statePopupNames, 2);
            to_stateValues.CopyTo(show_statePopupValues, 0);


        }

        private void InitTransitionNames(StateMachineDebugger smd)
        {
            if (!smd.transitionValid)
                return;

            to_triggerNames = new string[smd.triggers.Length];
            to_triggerValues = new int[smd.triggers.Length];

            for(int i = 0; i < to_triggerValues.Length; i++)
            {
                to_triggerValues[i] = i;
                to_triggerNames[i] = smd.triggers.GetValue(i).ToString();
            }
        }

        private bool ShouldShowTransitionOfIndex(int index)
        {
            return showTransitionOfState <= 1 || showTransitionOfState - 2 == index;
        }

        private bool OnlyShowLastActiveTransition
        {
            get { return showTransitionOfState == 1; }
        }
    }

    
}