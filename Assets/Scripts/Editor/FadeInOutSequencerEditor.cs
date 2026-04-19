#nullable enable

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FadeInOutSequencer))]
public class FadeInOutSequencerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space();

        var sequencer = (FadeInOutSequencer)target;

        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        if (GUILayout.Button("▶  Play Sequence"))
        {
            sequencer.PlaySequence();
        }
        EditorGUI.EndDisabledGroup();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to run the sequence.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

