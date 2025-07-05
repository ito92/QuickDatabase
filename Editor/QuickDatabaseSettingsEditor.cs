using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using DaBois.Utilities;

namespace DaBois.Settings.Editor
{
    [CustomEditor(typeof(QuickDatabaseGlobalSettings), true)]
    public class QuickDatabaseSettingsEditor : UnityEditor.Editor
    {
        private QuickDatabaseGlobalSettings _x;
        private SerializedProperty _assetManagement;
        static readonly string[] _excludedFields = { "m_Script" };

        private void OnEnable()
        {
            _x = (QuickDatabaseGlobalSettings)target;
            _assetManagement = serializedObject.FindProperty("_assetManagement");
        }

        public override void OnInspectorGUI() => DrawDefaultInspector();

        protected new bool DrawDefaultInspector()
        {
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Compiling...", MessageType.Info);
                return false;
            }

            if (serializedObject.targetObject == null) return false;

            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();

            //DrawPropertiesExcluding(serializedObject, _excludedFields);
#if QuickDatabaseSettings_Transition
            _assetManagement.enumValueIndex = 1;
#elif QuickDatabaseSettings_Addressables
            _assetManagement.enumValueIndex = 2;
#else
            _assetManagement.enumValueIndex = 0;
#endif
            EditorGUILayout.PropertyField(_assetManagement);
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                _x.Refresh((QuickDatabaseGlobalSettings.assetManagement)_assetManagement.enumValueIndex);
            }

            GUILayout.FlexibleSpace();
            GUI.enabled = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("This Asset", serializedObject.targetObject, typeof(QuickDatabaseGlobalSettings), false);
            GUI.enabled = true;

            if (GUILayout.Button("ping"))
            {
                EditorGUIUtility.PingObject(serializedObject.targetObject);
            }

            EditorGUILayout.EndHorizontal();

            return EditorGUI.EndChangeCheck();
        }

    }
}