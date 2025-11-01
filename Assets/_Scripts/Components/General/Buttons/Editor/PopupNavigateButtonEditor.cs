#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Components.General.Buttons;

[CustomEditor(typeof(PopupNavigateButton))]
public class PopupNavigateButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("PopupNavigateButton opens the selected EPopup when clicked.", MessageType.Info);
    }
}
#endif
