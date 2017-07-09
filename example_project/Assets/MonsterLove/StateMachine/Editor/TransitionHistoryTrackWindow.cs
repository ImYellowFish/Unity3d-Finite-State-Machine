using UnityEngine;
using UnityEditor;
using System.Collections;

namespace MonsterLove.StateMachine
{
    public class TransitionHistoryTrackWindow : EditorWindow
    {
        StateMachineDebugger.TransitionRecord record = new StateMachineDebugger.TransitionRecord { index = -1 };

        public void SetRecord(StateMachineDebugger.TransitionRecord record)
        {
            this.record = record;
        }


        void OnGUI()
        {
            if (record.index < 0)
                return;

            DrawStackTrace(record);

        }

        private Vector2 scrollPos;
        private void DrawStackTrace(StateMachineDebugger.TransitionRecord record)
        {
            if (record.index < 0)
                return;
            try
            {
                var trace = record.stackTrace;
                var frames = trace.GetFrames();

                DrawRecordInfo(record);

                Color defaultColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.white;

                StateMachineDebuggerEditorUtil.DrawSplitLine();
                GUILayout.BeginVertical();
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                foreach (var frame in frames)
                {
                    DrawStackFrame(frame);
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUI.backgroundColor = defaultColor;
            }
            catch
            {

            }     
        }

        private void DrawStackFrame(System.Diagnostics.StackFrame frame)
        {
            var method = frame.GetMethod();
            GUILayout.Label("class: " + method.ReflectedType.Name + ", method: " + method.ToString());
            GUILayout.Label("file: " + frame.GetFileName() + ", line " + frame.GetFileLineNumber().ToString());
            if (GUILayout.Button("Goto", GUILayout.Width(100)))
            {
                string fileName = frame.GetFileName();
                string fileAssetPath = fileName.Substring(fileName.IndexOf("Assets"));
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(fileAssetPath), frame.GetFileLineNumber());
            }
            GUILayout.Space(20);
        }

        private void DrawRecordInfo(StateMachineDebugger.TransitionRecord record)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Transition:");
            GUILayout.Label(record.transition.FromStateName + " -> " +
                record.transition.ToStateName + " (" + record.transition.TriggerName + ") ");
            GUILayout.EndVertical();
        }
    }
}