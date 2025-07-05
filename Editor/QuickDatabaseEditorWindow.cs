using DaBois.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static DaBois.EditorUtilities.QuickDatabaseEditor;

namespace DaBois.EditorUtilities
{
    public class QuickDatabaseEditorWindow : EditorWindow
    {
        private SerializedProperty _database;
        private Vector2 _scroll;
        private ReorderableList _reorderableList;
        private SerializedProperty _items;
        private SerializedObject _dataHolder;
        private ItemsHolder _itemsHolder;
        private SerializedProperty _dataHolderList;
        private Action _onClose;
        private bool _showPreviews;
        private Dictionary<int, Texture2D> _previews = new Dictionary<int, Texture2D>();
        private bool _previewsGenerated;
        private const int PREVIEW_SIZE = 48;

        public class ItemsHolder : ScriptableObject
        {
            public List<int> items = new List<int>();
        }

        public static void Open(SerializedProperty database, Action onClose)
        {
            QuickDatabaseEditorWindow window = CreateInstance<QuickDatabaseEditorWindow>();
            window.titleContent = new GUIContent("Database Editor");
            window._onClose = onClose;
            window._database = database;
            window._items = database.FindPropertyRelative("_items");
            window._itemsHolder = ScriptableObject.CreateInstance<ItemsHolder>();
            window._dataHolder = new SerializedObject(window._itemsHolder);            
            window._dataHolderList = window._dataHolder.FindProperty("items");

            List<OrderableData> reordered = new List<OrderableData>();
            for (int i = 0; i < window._items.arraySize; i++)
            {
                reordered.Add(new OrderableData() { id = window._items.GetArrayElementAtIndex(i).FindPropertyRelative("_id").intValue, order = window._items.GetArrayElementAtIndex(i).FindPropertyRelative("_order").intValue });
            }
            reordered.Sort(ItemsOrderer);

            for(int i = 0; i < reordered.Count; i++)
            {
                window._dataHolderList.InsertArrayElementAtIndex(i);
                //window._dataHolderList.GetArrayElementAtIndex(i).intValue = reordered[i].id;
            }
            for (int i = 0; i < reordered.Count; i++)
            {
                //window._dataHolderList.InsertArrayElementAtIndex(i);
                window._dataHolderList.GetArrayElementAtIndex(i).intValue = reordered[i].id;
            }
            window._dataHolder.ApplyModifiedProperties();

            window._reorderableList = new ReorderableList(window._dataHolder, window._dataHolderList, true, false, false, false);
            window._reorderableList.drawElementCallback += window.DrawReoderable;
            window._reorderableList.elementHeight = EditorGUIUtility.singleLineHeight * 3;
            window.Show();
        }

        private void OnDisable()
        {
            _onClose?.Invoke();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal("Toolbar");

            _showPreviews = EditorGUILayout.Toggle("", _showPreviews, "Button", GUILayout.Width(50));
            Rect labelRect = GUILayoutUtility.GetLastRect();
            GUI.Label(labelRect, "Previews");

            EditorGUILayout.EndHorizontal();

            if (!_showPreviews)
            {
                _previewsGenerated = false;
            }
            else if (!_previewsGenerated)
            {
                _previewsGenerated = true;
                _previews.Clear();
                Editor tempEditor;
                for(int i = 0; i < _items.arraySize; i++)
                {
                    SerializedProperty prefab = _items.GetArrayElementAtIndex(i).FindPropertyRelative("_prefab");
                    if (prefab.objectReferenceValue)
                    {
                        tempEditor = Editor.CreateEditor(prefab.objectReferenceValue);
                        _previews.Add(_items.GetArrayElementAtIndex(i).FindPropertyRelative("_id").intValue, tempEditor.RenderStaticPreview(AssetDatabase.GetAssetPath(prefab.objectReferenceValue), null, PREVIEW_SIZE, PREVIEW_SIZE));
                        DestroyImmediate(tempEditor);
                    }
                }
            }

            List<OrderableData> reordered = new List<OrderableData>();
            for (int i = 0; i < _items.arraySize; i++)
            {
                reordered.Add(new OrderableData() { id = _items.GetArrayElementAtIndex(i).FindPropertyRelative("_id").intValue, order = _items.GetArrayElementAtIndex(i).FindPropertyRelative("_order").intValue });
            }
            reordered.Sort(ItemsOrderer);

            Dictionary<int, int> reorderedKeys = new Dictionary<int, int>();
            for (int i = 0; i < reordered.Count; i++)
            {
                OrderableData order = reordered[i];
                order.order = i;
                reorderedKeys.Add(order.id, order.order);
                //reorderedKeysR.Add(order.order, order.id);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _dataHolder.Update();            
            _reorderableList.DoLayoutList();
            _dataHolder.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                _reorderableList.serializedProperty.serializedObject.ApplyModifiedProperties();
                _dataHolder.Update();
                for (int i = 0; i < _items.arraySize; i++)
                {
                    int id = _dataHolderList.GetArrayElementAtIndex(i).intValue;
                    _items.GetArrayElementAtIndex(id).FindPropertyRelative("_order").intValue = i;
                }
            }
            
            for (int i = 0; i < _items.arraySize; i++)
            {
                int id = reorderedKeys[i];
                //SerializedProperty item = _items.GetArrayElementAtIndex(id);
                //EditorGUILayout.PropertyField(reorderableList.serializedProperty.GetArrayElementAtIndex(id));                
                //DrawItem(item);
            }
            EditorGUILayout.EndScrollView();

            _database.serializedObject.ApplyModifiedProperties();
        }

        private void DrawReoderable(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty item = _items.GetArrayElementAtIndex(_dataHolderList.GetArrayElementAtIndex(index).intValue);
            //SerializedProperty icon = item.FindPropertyRelative("_icon");
            SerializedProperty icon = item.FindPropertyRelative("_iconAsset");
            rect.height = EditorGUIUtility.singleLineHeight * 3;
            rect.width = rect.height + EditorGUIUtility.singleLineHeight * .5f;

            Rect rightRect = new Rect(rect);
            rightRect.x += EditorGUIUtility.singleLineHeight * 4;
            rightRect.height = EditorGUIUtility.singleLineHeight;
            //icon.objectReferenceValue = EditorGUI.ObjectField(rect, icon.objectReferenceValue, typeof(Sprite), allowSceneObjects: false);
            Sprite iconObject = LoadAssetByGUID<Sprite>(icon.FindPropertyRelative("m_AssetGUID").stringValue);
            EditorGUI.ObjectField(rect, iconObject, typeof(Sprite), allowSceneObjects: false);

            SerializedProperty order = item.FindPropertyRelative("_order");
            SerializedProperty name = item.FindPropertyRelative("_name");

            //EditorGUI.LabelField(rightRect, "", order.intValue.ToString(), "AssetLabel");
            order.intValue = EditorGUI.DelayedIntField(rightRect, "", order.intValue, "AssetLabel");
            rightRect.width = EditorGUIUtility.labelWidth;
            rightRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.TextField(rightRect, "", name.stringValue);
            rightRect.width = position.width;

            if (_showPreviews)
            {
                Rect previewRect = new Rect(rect);
                previewRect.x += PREVIEW_SIZE + EditorGUIUtility.labelWidth + rect.width;
                previewRect.width = previewRect.height = PREVIEW_SIZE;
                EditorGUI.DrawPreviewTexture(previewRect, _previews[item.FindPropertyRelative("_id").intValue]);
            }
        }

        public static T LoadAssetByGUID<T>(string guid) where T : UnityEngine.Object
        {
            return (T)AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}