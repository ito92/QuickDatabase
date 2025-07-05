using DaBois.Utilities;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace DaBois.Utilities
{
    public class QuickDatabaseGlobalSettings : ScriptableObject
    {
        public enum assetManagement { Direct, Transition, Addressables }

        [SerializeField]
        private assetManagement _assetManagement = assetManagement.Direct;

        public static QuickDatabaseGlobalSettings Instance => _instance != null ? _instance : Initialize();
        private static QuickDatabaseGlobalSettings _instance;

#if !UNITY_EDITOR
        private void OnEnable()
        {
            RuntimeInit();
        }
#endif
        private const string displayPath = "QuickDatabase/Settings";
        private const string filename = "QuickDatabaseSettings";
        private const string title = "QuickDatabase Settings";
        private readonly string[] tags = new string[] { "Quick", "Database", "Settings" };

        public static string _preDefine = "QuickDatabaseSettings_";
        public static string _transitionDefineLabel = "Transition";
        public static string _addressablesDefineLabel = "Addressables";

        public void RuntimeInit()
        {
            _instance = this;
            //Refresh();
        }

        private void OnValidate()
        {
            //Refresh();
        }

        public void Refresh()
        {
            Debug.Log("Refreshin QuickDatabase Defines: " + _assetManagement);
#if UNITY_EDITOR
            AddDefine(_assetManagement == assetManagement.Transition, _transitionDefineLabel);
            AddDefine(_assetManagement == assetManagement.Addressables, _addressablesDefineLabel);
#endif
        }

        public void Refresh(assetManagement management)
        {            
#if UNITY_EDITOR
            _assetManagement = management;
            Debug.Log("Refreshin QuickDatabase Defines: " + _assetManagement);
            AddDefine(_assetManagement == assetManagement.Transition, _transitionDefineLabel);
            AddDefine(_assetManagement == assetManagement.Addressables, _addressablesDefineLabel);
#endif
        }

        protected static QuickDatabaseGlobalSettings Initialize()
        {
            if (_instance != null)
            {
                return _instance;
            }

            // Attempt to load the settings asset.
            var path = GetSettingsPath() + filename + ".asset";

#if UNITY_EDITOR
            _instance = AssetDatabase.LoadAssetAtPath<QuickDatabaseGlobalSettings>(path);
            if (_instance != null)
            {
                return _instance;
            }

            //Move asset to the correct path if already exists
            var instances = Resources.FindObjectsOfTypeAll<QuickDatabaseGlobalSettings>();
            if (instances.Length > 0)
            {
                var oldPath = AssetDatabase.GetAssetPath(instances[0]);
                var result = AssetDatabase.MoveAsset(oldPath, path);

                if (oldPath == path)
                {
                    Debug.Log(instances[0] + " is in the correct path. Skipping moving");
                    _instance = instances[0];
                    return _instance;
                }
                else if (string.IsNullOrEmpty(result))
                {
                    _instance = instances[0];
                    return _instance;
                }
                else
                {
                    Debug.LogWarning($"Failed to move previous settings asset " + $"'{oldPath}' to '{path}'. " + $"A new settings asset will be created.", _instance);
                }
            }
            if (_instance != null)
            {
                return _instance;
            }
            _instance = CreateInstance<QuickDatabaseGlobalSettings>();
#endif

#if UNITY_EDITOR
            Directory.CreateDirectory(Path.Combine(
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName(path)));

            AssetDatabase.CreateAsset(_instance, path);
            AssetDatabase.Refresh();
#endif
            return _instance;
        }

        static string GetSettingsPath()
        {
            return "Assets/Settings/";
        }

#if UNITY_EDITOR
        private static Editor _editor;

        public SettingsProvider GenerateProvider()
        {
            var provider = new SettingsProvider(displayPath, SettingsScope.Project)
            {
                label = title,
                guiHandler = (searchContext) =>
                {
                    var settings = Instance;

                    if (!_editor)
                    {
                        _editor = Editor.CreateEditor(Instance);
                    }
                    _editor.OnInspectorGUI();
                },

                keywords = tags
            };

            return provider;
        }

        private void AddDefine(bool add, string name)
        {
            if (add)
            {
                AddDefineIfNecessary(_preDefine + name, BuildTargetGroup.Standalone);
                AddDefineIfNecessary(_preDefine + name, BuildTargetGroup.Android);
                AddDefineIfNecessary(_preDefine + name, BuildTargetGroup.iOS);
            }
            else
            {
                RemoveDefineIfNecessary(_preDefine + name, BuildTargetGroup.Standalone);
                RemoveDefineIfNecessary(_preDefine + name, BuildTargetGroup.Android);
                RemoveDefineIfNecessary(_preDefine + name, BuildTargetGroup.iOS);
            }
        }

        public static void AddDefineIfNecessary(string _define, BuildTargetGroup _buildTargetGroup)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(_buildTargetGroup);

            if (defines == null) { defines = _define; }
            else if (defines.Length == 0) { defines = _define; }
            else { if (defines.IndexOf(_define, 0) < 0) { defines += ";" + _define; } }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(_buildTargetGroup, defines);
        }

        public static void RemoveDefineIfNecessary(string _define, BuildTargetGroup _buildTargetGroup)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(_buildTargetGroup);

            if (defines.StartsWith(_define + ";"))
            {
                // First of multiple defines.
                defines = defines.Remove(0, _define.Length + 1);
            }
            else if (defines.StartsWith(_define))
            {
                // The only define.
                defines = defines.Remove(0, _define.Length);
            }
            else if (defines.EndsWith(";" + _define))
            {
                // Last of multiple defines.
                defines = defines.Remove(defines.Length - _define.Length - 1, _define.Length + 1);
            }
            else
            {
                // Somewhere in the middle or not defined.
                var index = defines.IndexOf(_define, 0, System.StringComparison.Ordinal);
                if (index >= 0) { defines = defines.Remove(index, _define.Length + 1); }
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(_buildTargetGroup, defines);
        }
#endif
    }
}

#if UNITY_EDITOR
static class QuickDatabaseSettingsRegister
{
    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        return QuickDatabaseGlobalSettings.Instance.GenerateProvider();
    }
}
#endif