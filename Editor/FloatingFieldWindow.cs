using UnityEditor;
using UnityEngine;

namespace DaBois.EditorUtilities
{
    public class FloatingFieldWindow : PopupWindowContent
    {
        private static SerializedProperty _property;
        private static Rect _position;

        public static void Open(SerializedProperty property, Rect position)
        {
            _property = property;
            _position = position;
            PopupWindow.Show(_position, new FloatingFieldWindow());
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.PropertyField(_property, new GUIContent());
            if (EditorGUI.EndChangeCheck())
            {
                _property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        public override void OnClose()
        {
            base.OnClose();
            _property = null;
        }

        public override Vector2 GetWindowSize()
        {
            return _position.size;
        }
    }
}