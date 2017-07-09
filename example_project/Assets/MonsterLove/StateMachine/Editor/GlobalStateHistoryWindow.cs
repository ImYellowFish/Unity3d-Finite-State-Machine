using UnityEngine;
using UnityEditor;
using System.Collections;

namespace MonsterLove.StateMachine
{
    
    public class GlobalStateHistoryWindow : EditorWindow
    {
        // Add menu item to show this window.
        [MenuItem("FSM/Global FSM History")]
        public static void Create()
        {
            var window = GetWindow(typeof(GlobalStateHistoryWindow));
            window.titleContent = new GUIContent("G_FSM_History");
            window.Show();
        }

        private int historyCount = -1;
        void Update()
        {
            try
            {
                int newCount = StateMachineDebugger.g_transitionHistory.Count;
                if (newCount != historyCount)
                    Repaint();
                historyCount = newCount;
            }
            catch
            {

            }
        }

        private static bool timeAscending = true;
        private Vector2 scrollPos;
        private void OnGUI()
        {
            try
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("History", GUILayout.Width(100));
                GUILayout.Label("Time Ascending", GUILayout.Width(100));
                timeAscending = EditorGUILayout.Toggle(timeAscending);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                
                var g_history = StateMachineDebugger.g_transitionHistory;

                StateMachineDebuggerEditorUtil.DrawTransitionHistory(g_history, -1, ref scrollPos, !timeAscending, true);
                
            }
            catch
            {

            }
        }
    }
}