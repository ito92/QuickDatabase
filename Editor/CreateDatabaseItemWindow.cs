using UnityEditor;
using UnityEngine;
using System.Reflection;
using DaBois.Utilities;

namespace DaBois.EditorUtilities
{
    public class CreateDatabaseItemWindow : EditorWindow
    {
        private SerializedProperty _items;
        private SerializedProperty _itemProp;
        private System.Action<SerializedProperty> _onCreation;
        private bool _success;
        private string _name;

        public static void Open(SerializedProperty items, System.Action<SerializedProperty> callback)
        {
            CreateDatabaseItemWindow window = GetWindow<CreateDatabaseItemWindow>(true, "Create New Item");
            items.arraySize++;
            window._itemProp = items.GetArrayElementAtIndex(items.arraySize - 1);
            window._itemProp.isExpanded = true;

            SerializedProperty childProp = window._itemProp.Copy();
            SerializedProperty endProperty = childProp.GetEndProperty();

            if (childProp.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(childProp, endProperty)) break;

                    //Debug.Log($"Field: {childProp.name}, Type: {childProp.propertyType}");
                    ResetValue(childProp);
                }
                while (childProp.NextVisible(true));
            }

            window._itemProp.FindPropertyRelative("_order").intValue = 10000;
            window._itemProp.FindPropertyRelative("_notValid").boolValue = true;
            window._items = items;
            window._onCreation = callback;

            window.Show();
        }

        static void ResetValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = 0; break;
                case SerializedPropertyType.Float: prop.floatValue = 0f; break;
                case SerializedPropertyType.String: prop.stringValue = string.Empty; break;
                case SerializedPropertyType.Boolean: prop.boolValue = false; break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = null; break;
                case SerializedPropertyType.Vector3: prop.vector3Value = Vector3.zero; break;
            }
        }

        private void OnDestroy()
        {
            if (!_success)
            {
                Debug.Log("Creation failed");
                _items.arraySize--;
            }
            _onCreation?.Invoke(null);
        }

        private void OnGUI()
        {
            if (_itemProp == null) { Close(); return; }
            EditorGUILayout.PropertyField(_itemProp, true);
            _itemProp.serializedObject.ApplyModifiedProperties();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Height(30)))
            {
                _itemProp.FindPropertyRelative("_notValid").boolValue = false;
                _itemProp.FindPropertyRelative("_order").intValue = 10000;
                _success = true;
                _onCreation?.Invoke(_itemProp);
                Close();
            }
        }

        public class Container : ScriptableObject
        {
            public QuickDatabaseItem item;
        }
    }
}
