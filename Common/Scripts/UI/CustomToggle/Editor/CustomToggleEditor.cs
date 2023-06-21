#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(CustomToggle), true)]
[CanEditMultipleObjects]
public class CustomToggleEditor : Editor
{
    public SerializedProperty onValueChangedProperty;

    void OnEnable()
    {
        onValueChangedProperty = serializedObject.FindProperty("onValueChanged");
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();

        CustomToggle thisTarget = (CustomToggle)target;

        EditorGUI.BeginChangeCheck();
        thisTarget.graphic = (Graphic)EditorGUILayout.ObjectField("Graphic : ", thisTarget.graphic, typeof(Graphic), true);
        thisTarget.graphic_OFF = (Graphic)EditorGUILayout.ObjectField("Graphic OFF : ", thisTarget.graphic_OFF, typeof(Graphic), true);
        thisTarget.isOn = EditorGUILayout.Toggle("isOn", thisTarget.isOn);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.PropertyField(onValueChangedProperty);
        serializedObject.ApplyModifiedProperties();
    }
}

#endif //UNITY_EDITOR
