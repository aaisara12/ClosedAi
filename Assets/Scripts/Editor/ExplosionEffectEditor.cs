#nullable enable

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ExplosionEffect))]
public class ExplosionEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space();

        var effect = (ExplosionEffect)target;

        EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying);
        if (GUILayout.Button("▶  Play"))
        {
            effect.Play();
        }
        if (GUILayout.Button("■  Stop"))
        {
            effect.Stop();
        }
        EditorGUI.EndDisabledGroup();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to trigger the explosion.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

