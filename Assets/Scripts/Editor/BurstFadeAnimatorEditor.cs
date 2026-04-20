#nullable enable

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BurstFadeAnimator))]
public class BurstFadeAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space();

        var animator = (BurstFadeAnimator)target;

        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        if (GUILayout.Button("▶  Play"))
        {
            animator.Play();
        }
        EditorGUI.EndDisabledGroup();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to trigger the burst fade.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

