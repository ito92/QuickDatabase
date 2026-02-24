using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DaBois.Utilities;
using System;
using static DaBois.Utilities.QuickDatabaseGlobalSettings;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;

namespace DaBois.EditorUtilities
{
    [CustomPropertyDrawer(typeof(QuickDatabase), true)]
    public class QuickDatabaseEditor : PropertyDrawer
    {
        public enum action { None, Add, RemoveLast, RemoveId, Create }

        public struct OrderableData
        {
            public int id;
            public int order;
        }

        private int _toolbarItem = 1;
#if QuickDatabaseSettings_Transition
        private string[] _toolbarOptions = new string[] { "Original Order", "Custom Order", "Open Editor", "Perform Transition" };
#elif QuickDatabaseSettings_Addressables
        private string[] _toolbarOptions = new string[] { "Original Order", "Custom Order", "Open Editor" };
#else
        private string[] _toolbarOptions = new string[] { "Original Order", "Custom Order", "Open Editor" };
#endif

        private bool _init;
        private List<OrderableData> _reordered = new List<OrderableData>();
        private Dictionary<int, int> _reorderedKeys = new Dictionary<int, int>();
        private Dictionary<int, int> _reorderedKeysR = new Dictionary<int, int>();
        protected List<SerializedProperty> _itemsList = new List<SerializedProperty>();
        protected string _searchFilter;
        private string[] _paneOptions = { "Auto Assing Icons" };
        private int _currentPage;
        private action _action;
        private object _actionData;
        private bool _dragging;
        private readonly Color _acceptColor = new Color(.01f, .98f, .32f, .5f);
        private readonly Color _rejectColor = new Color(.88f, .06f, .16f, .5f);
        private bool _popupOpen;

        private const string SETTINGS_PATH = "QuickDatabaseSettings";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BaseGUI(position, property, label);
        }

        private void BaseInit(SerializedProperty property)
        {
            if (_init)
            {
                return;
            }
            if (_popupOpen) return;

            _init = true;            

            GUI.changed = true;
            SerializedProperty _items = property.FindPropertyRelative("_items");
            for (int i = 0; i < _items.arraySize; i++)
            {
                _items.GetArrayElementAtIndex(i).FindPropertyRelative("_id").intValue = i;
            }

            SerializedProperty editorSettings = property.FindPropertyRelative("_editorSettings");
            if (string.IsNullOrEmpty(editorSettings.FindPropertyRelative("_id").stringValue))
            {
                editorSettings.FindPropertyRelative("_id").stringValue = Guid.NewGuid().ToString();
                property.serializedObject.ApplyModifiedProperties();
            }

            _currentPage = EditorPrefs.GetInt(SETTINGS_PATH + "/" + editorSettings.FindPropertyRelative("_id").stringValue);

            Init(property);
        }

        protected virtual void Init(SerializedProperty property)
        {

        }

        protected void BaseGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect startPosition = position;
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property, label, false);
            if (property.isExpanded)
            {
                assetManagement assetsManagement = default;
#if QuickDatabaseSettings_Transition
                assetsManagement = assetManagement.Transition;
#elif QuickDatabaseSettings_Addressables
                assetsManagement = assetManagement.Addressables;
#else
                assetsManagement = assetManagement.Direct;
#endif

                property.serializedObject.Update();

                BaseInit(property);

                int pages = property.FindPropertyRelative("_editorSettings").FindPropertyRelative("ItemsPerPage").intValue;
                if (pages <= 0)
                {
                    pages = 20;
                }
                QuickDatabaseEditorSettings settings = new QuickDatabaseEditorSettings(pages);
                SerializedProperty editorSettings = property.FindPropertyRelative("_editorSettings");

                EditorGUI.indentLevel++;
                position.x += 16;
                position.width -= 16;
                position.height = EditorGUIUtility.singleLineHeight;
                position.y += EditorGUIUtility.singleLineHeight * 1;
                int oldToolbarItem = _toolbarItem;
                _toolbarItem = GUI.Toolbar(position, _toolbarItem, _toolbarOptions);

                if (_toolbarItem == 2)
                {
                    _toolbarItem = oldToolbarItem;
                    QuickDatabaseEditorWindow.Open(property, OnEditorClose);
                }
                else if (_toolbarItem == 3)
                {
                    _toolbarItem = oldToolbarItem;
                    PerformTransition(property);
                }

                position.y += EditorGUIUtility.singleLineHeight;

                SerializedProperty _items = property.FindPropertyRelative("_items");

                if (GUI.changed)
                {
                    //Debug.Log("UI Changed");
                    _itemsList.Clear();
                    for (int i = 0; i < _items.arraySize; i++)
                    {
                        _itemsList.Add(_items.GetArrayElementAtIndex(i));
                    }
                }

                GUI.Box(position, "", "Toolbar");
                ToolbarGUI(position, new QuickDatabaseEditorSettings(pages));

                position.y += EditorGUIUtility.singleLineHeight * 1.5f;

                if (GUI.changed)
                {
                    //Adjust order
                    _reordered.Clear();
                    for (int i = 0; i < _items.arraySize; i++)
                    {
                        _reordered.Add(new OrderableData() { id = _itemsList[i].FindPropertyRelative("_id").intValue, order = _itemsList[i].FindPropertyRelative("_order").intValue });
                    }
                    _reordered.Sort(ItemsOrderer);

                    _reorderedKeys.Clear();
                    _reorderedKeysR.Clear();

                    for (int i = 0; i < _reordered.Count; i++)
                    {
                        OrderableData order = _reordered[i];
                        order.order = i;
                        _reorderedKeys.Add(order.id, order.order);
                        _reorderedKeysR.Add(order.order, order.id);
                    }

                    for (int i = 0; i < _items.arraySize; i++)
                    {
                        _itemsList[i].FindPropertyRelative("_order").intValue = _reorderedKeys[i];
                    }
                }

                bool filtering = !string.IsNullOrEmpty(_searchFilter);
                int renderId = 0;
                int startItem = settings.ItemsPerPage * _currentPage;
                int lastItem = startItem + Math.Min(settings.ItemsPerPage, _reordered.Count);

                for (int i = startItem; i < lastItem; i++)
                {
                    int id = i;

                    if (i >= _reordered.Count)
                    {
                        break;
                    }

                    if (_toolbarItem == 1)
                    {
                        id = _reordered[i].id;
                    }

                    if (filtering)
                    {
                        bool passed = false;
                        if (_itemsList[id].FindPropertyRelative("_order").intValue.ToString().Contains(_searchFilter))
                        {
                            passed = true;
                        }
                        else if (_itemsList[id].FindPropertyRelative("_name").stringValue.ContainsInvariantCultureIgnoreCase(_searchFilter))
                        {
                            passed = true;
                        }
                        else
                        {
                            Filter(_itemsList[id], ref passed);
                        }

                        if (!passed)
                        {
                            lastItem++;
                            continue;
                        }
                    }

                    if (renderId > 0)
                    {
                        position.y += ItemHeight(assetsManagement);
                        position.height = ItemHeight(assetsManagement);
                    }
                    renderId++;

                    DrawItem(id, ref position);
                }

                position.x = startPosition.x + 16;
                position.y += ItemHeight(assetsManagement);
                GUI.Box(position, "", "Toolbar");
                FooterGUI(position, settings);

                EditorGUI.indentLevel--;

                switch (_action)
                {
                    case action.Add:
                        _items.InsertArrayElementAtIndex(_items.arraySize);
                        _items.GetArrayElementAtIndex(_items.arraySize - 1).FindPropertyRelative("_order").intValue = 10000;
                        GUI.changed = true;
                        break;
                    case action.RemoveLast:
                        _items.DeleteArrayElementAtIndex(_items.arraySize - 1);
                        GUI.changed = true;
                        break;
                    case action.RemoveId:
                        _items.DeleteArrayElementAtIndex((int)_actionData);
                        GUI.changed = true;
                        break;
                    case action.Create:
                        _popupOpen = true;
                        CreateItemWindow(_items, (s)=>
                        {
                            _popupOpen = false;
                            _items.serializedObject.ApplyModifiedProperties();
                            GUI.changed = true;
                            property.serializedObject.Update();
                        });
                        break;
                }

                property.serializedObject.ApplyModifiedProperties();

                EditorPrefs.SetInt(SETTINGS_PATH + "/" + editorSettings.FindPropertyRelative("_id").stringValue, _currentPage);

                if (_action != action.None)
                {
                    _actionData = null;
                    _action = action.None;
                    GUI.changed = true;
                    _init = false;
                }
            }
            else
            {
                _init = false;
            }
        }

        private void SetPropertyValue(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = (int)value; break;
                case SerializedPropertyType.Float: prop.floatValue = (float)value; break;
                case SerializedPropertyType.String: prop.stringValue = (string)value; break;
                case SerializedPropertyType.Boolean: prop.boolValue = (bool)value; break;
                case SerializedPropertyType.Color: prop.colorValue = (Color)value; break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = (UnityEngine.Object)value; break;
            }
        }

        protected virtual void CreateItemWindow(SerializedProperty items, System.Action<SerializedProperty> callback)
        {
            CreateDatabaseItemWindow.Open(items, callback);
        }

        protected virtual void Filter(SerializedProperty item, ref bool passed)
        {
            
        }

        protected virtual void ToolbarGUI(Rect position, QuickDatabaseEditorSettings settings)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            position.y += 1;
            position.width = 100f;
            _searchFilter = EditorGUI.TextField(position, "", _searchFilter, "SearchTextField");

            position.x += position.width + 5;
            position.width = 16;
            if (GUI.Button(position, new GUIContent("+", "Add"), "ButtonMid"))
            {
                _action = action.Add;
            }
            position.x += 16;
            if (GUI.Button(position, new GUIContent("-", "Remove Last"), "ButtonMid"))
            {
                _action = action.RemoveLast;
            }
            position.x += 16;
            position.width = 36;
            if (GUI.Button(position, new GUIContent("New", "Create Item"), "ButtonMid"))
            {
                _action = action.Create;
            }

            position.x += position.width;
            position.width = 32;
            int chosenOption = EditorGUI.Popup(position, -1, _paneOptions, "PaneOptions");

            switch (chosenOption)
            {
                //Auto assign icons
                case 0:
                    string path = EditorUtility.OpenFolderPanel("Select folder containing the icons", "Assets", "");
                    path = path.Substring(path.IndexOf("Assets"));
                    string[] assets = AssetDatabase.FindAssets("t:Sprite", new string[] { path });
                    Sprite[] sprites = new Sprite[assets.Length];
                    for (int i = 0; i < assets.Length; i++)
                    {
                        //Debug.Log(assets[i]);
                        sprites[i] = (Sprite)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[i]), typeof(Sprite));
                        //Debug.Log(sprites[i], sprites[i]);
                    }

                    for (int i = 0; i < _itemsList.Count; i++)
                    {
                        string name = _itemsList[i].FindPropertyRelative("_name").stringValue;
                        for (int j = 0; j < sprites.Length; j++)
                        {
                            if (sprites[j].name.ContainsInvariantCultureIgnoreCase(name))
                            {
                                _itemsList[i].FindPropertyRelative("_icon").objectReferenceValue = sprites[j];
                                break;
                            }
                        }
                    }

                    break;
            }

            position.width = 36;
            position.x += 32;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 36;
            _currentPage = EditorGUI.IntField(position, "", _currentPage + 1) - 1;
            position.x += 20;
            EditorGUI.PrefixLabel(position, new GUIContent("/" + ((_itemsList.Count / settings.ItemsPerPage) + 1)));
            position.x += 45;
            position.width = 16;
            if (GUI.Button(position, "", "dragtab scroller prev"))
            {
                _currentPage--;
            }
            position.x += 16;
            if (GUI.Button(position, "", "dragtab scroller next"))
            {
                _currentPage++;
            }

            _currentPage = Mathf.Clamp(_currentPage, 0, _itemsList.Count / settings.ItemsPerPage);

            EditorGUIUtility.labelWidth = labelWidth;

        }

        protected virtual void FooterGUI(Rect position, QuickDatabaseEditorSettings settings)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            position.width = 16;
            if (GUI.Button(position, new GUIContent("+", "Add"), "ButtonMid"))
            {
                _action = action.Add;
            }
            position.x += 16;
            if (GUI.Button(position, new GUIContent("-", "Remove Last"), "ButtonMid"))
            {
                _action = action.RemoveLast;
            }
            position.x += 16;
            position.width = 36;
            if (GUI.Button(position, new GUIContent("New", "Create Item"), "ButtonMid"))
            {
                _action = action.Create;
            }

            position.width = 36;
            position.x += 32;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 36;
            _currentPage = EditorGUI.IntField(position, "", _currentPage + 1) - 1;
            position.x += 20;
            EditorGUI.PrefixLabel(position, new GUIContent("/" + ((_itemsList.Count / settings.ItemsPerPage) + 1)));
            position.x += 45;
            position.width = 16;
            if (GUI.Button(position, "", "dragtab scroller prev"))
            {
                _currentPage--;
            }
            position.x += 16;
            if (GUI.Button(position, "", "dragtab scroller next"))
            {
                _currentPage++;
            }

            _currentPage = Mathf.Clamp(_currentPage, 0, _itemsList.Count / settings.ItemsPerPage);

            EditorGUIUtility.labelWidth = labelWidth;
        }

        private void OnEditorClose()
        {
            GUI.changed = true;
        }

        protected virtual void ItemContextMenu(int item, ref GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Delete"), false, DeleteItem, item);
        }

        private void DeleteItem(object item)
        {
            _action = action.RemoveId;
            _actionData = item;
        }

        public static int ItemsOrderer(OrderableData x, OrderableData y)
        {
            return x.order.CompareTo(y.order);
        }

        protected virtual void DrawItem(int item, ref Rect position)
        {
            if (Event.current.type == EventType.ContextClick && position.Contains(Event.current.mousePosition))
            {
                GenericMenu menu = new GenericMenu();

                ItemContextMenu(item, ref menu);
                menu.ShowAsContext();

                Event.current.Use();
            }


            float originalWidth = position.width;
            position.height = EditorGUIUtility.singleLineHeight * 3;
            position.width = EditorGUIUtility.singleLineHeight * 3.5f;
            assetManagement assetsManagement = default;

#if QuickDatabaseSettings_Transition
            assetsManagement = assetManagement.Transition;            
            SerializedProperty icon = _itemsList[item].FindPropertyRelative("_icon");
            SerializedProperty _iconAsset = _itemsList[item].FindPropertyRelative("_iconAsset");
            Rect fixedPos = new Rect(position);
            fixedPos.width /= 2;
            fixedPos.height /= 2;
            icon.objectReferenceValue = EditorGUI.ObjectField(fixedPos, icon.objectReferenceValue, typeof(Sprite), allowSceneObjects: false);
            fixedPos.y += fixedPos.height;
            Texture2D addressableIcon = null;

            string m_CachedAsset = _iconAsset.FindPropertyRelative("m_AssetGUID").stringValue;
            if (!string.IsNullOrEmpty(m_CachedAsset))
            {
                addressableIcon = ((Sprite)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_CachedAsset), typeof(Sprite))).texture;
            }
            EditorGUI.DrawPreviewTexture(fixedPos, addressableIcon ? addressableIcon : Texture2D.whiteTexture);
            if(Event.current.type == EventType.MouseUp && fixedPos.Contains(Event.current.mousePosition))
            {
                fixedPos.height = EditorGUIUtility.singleLineHeight * 1.2f;
                fixedPos.width *= 8;
                FloatingFieldWindow.Open(_iconAsset, fixedPos);
            }
#elif QuickDatabaseSettings_Addressables
            assetsManagement = assetManagement.Addressables;
            SerializedProperty _iconAsset = _itemsList[item].FindPropertyRelative("_iconAsset");
            Rect fixedPos = new Rect(position);

            Texture2D addressableIcon = RenderIcon(item);
            
            EditorGUI.DrawPreviewTexture(fixedPos, addressableIcon ? addressableIcon : Texture2D.whiteTexture, null, ScaleMode.ScaleAndCrop);
            if (Event.current.type == EventType.MouseUp && fixedPos.Contains(Event.current.mousePosition))
            {
                fixedPos.height = EditorGUIUtility.singleLineHeight * 1.2f;
                fixedPos.width *= 8;
                FloatingFieldWindow.Open(_iconAsset, fixedPos);
            }
            else
            {
                IconDragRoutine(fixedPos, _iconAsset);
            }
#else
            assetsManagement = assetManagement.Direct;
            SerializedProperty icon = _itemsList[item].FindPropertyRelative("_icon");
            icon.objectReferenceValue = EditorGUI.ObjectField(position, icon.objectReferenceValue, typeof(Sprite), allowSceneObjects: false);
#endif


            Rect rightRect = new Rect(position);

            rightRect.x += EditorGUIUtility.singleLineHeight * 3;
            rightRect.height = EditorGUIUtility.singleLineHeight;

            position.width = originalWidth - (position.height + EditorGUIUtility.singleLineHeight * 1f);
            DrawItemExtra(_itemsList[item], ref position, ref rightRect, assetsManagement);
            position.width = originalWidth;
        }

        protected virtual Texture2D RenderIcon(int item)
        {
            SerializedProperty _iconAsset = _itemsList[item].FindPropertyRelative("_iconAsset");

            string m_CachedAsset = _iconAsset.FindPropertyRelative("m_AssetGUID").stringValue;
            if (!string.IsNullOrEmpty(m_CachedAsset))
            {
                string path = AssetDatabase.GUIDToAssetPath(m_CachedAsset);
                if (!string.IsNullOrEmpty(path))
                {
                    return ((Sprite)AssetDatabase.LoadAssetAtPath(path, typeof(Sprite))).texture;
                }
            }

            return null;
        }

        private void IconDragRoutine(Rect drop_area, SerializedProperty iconAsset)
        {
            bool inArea = drop_area.Contains(Event.current.mousePosition);

            if (inArea && Event.current.type == EventType.DragUpdated)
            {
                Event.current.Use();

                _dragging = true;
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                _dragging = false;
                if (inArea)
                {
                    bool addedNew = false;
                    DragAndDrop.AcceptDrag();

                    string dragablePath = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]);
                    Sprite dragableIcon = (Sprite)AssetDatabase.LoadAssetAtPath(dragablePath, typeof(Sprite));
                    if (dragableIcon)
                    {
                        addedNew = true;
                        iconAsset.FindPropertyRelative("m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(dragablePath);

                        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                        if (settings.FindAssetEntry(iconAsset.FindPropertyRelative("m_AssetGUID").stringValue) == null)
                        {
                            settings.CreateAssetReference(iconAsset.FindPropertyRelative("m_AssetGUID").stringValue);
                        }
                    }

                    if (addedNew)
                    {
                        iconAsset.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
            else if (Event.current.type == EventType.DragExited)
            {
                _dragging = false;
            }

            if (_dragging)
            {
                if (drop_area.Contains(Event.current.mousePosition))
                {
                    string dragablePath = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]);
                    Sprite dragableIcon = (Sprite)AssetDatabase.LoadAssetAtPath(dragablePath, typeof(Sprite));
                    bool valid = dragableIcon;
                    EditorGUI.DrawRect(drop_area, valid ? _acceptColor : _rejectColor);
                    DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                }
            }
        }

        protected virtual void DrawItemExtra(SerializedProperty item, ref Rect position, ref Rect rightRect, assetManagement assetsManagement)
        {
            SerializedProperty order = item.FindPropertyRelative("_order");
            SerializedProperty name = item.FindPropertyRelative("_name");

            EditorGUI.LabelField(rightRect, "", order.intValue.ToString(), "AssetLabel");
            rightRect.width = EditorGUIUtility.labelWidth;
            rightRect.y += EditorGUIUtility.singleLineHeight;
            name.stringValue = EditorGUI.TextField(rightRect, "", name.stringValue);
            rightRect.width = position.width;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            assetManagement assetsManagement = default;
#if QuickDatabaseSettings_Transition
            assetsManagement = assetManagement.Transition;
#elif QuickDatabaseSettings_Addressables
            assetsManagement = assetManagement.Addressables;
#else
            assetsManagement = assetManagement.Direct;
#endif

            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            height += EditorGUIUtility.singleLineHeight * 3;
            SerializedProperty _items = property.FindPropertyRelative("_items");

            SerializedProperty editorSettings = property.FindPropertyRelative("_editorSettings");

            int itemsInPages = editorSettings.FindPropertyRelative("ItemsPerPage").intValue;
            if (itemsInPages <= 0)
            {
                itemsInPages = 20;
            }

            height += itemsInPages * (ItemHeight(assetsManagement) - 1);

            height += EditorGUIUtility.singleLineHeight * 3f;

            return height;
        }

        protected virtual float ItemHeight(assetManagement assetsManagement)
        {
            return EditorGUIUtility.singleLineHeight * 3.5f;
        }

        protected virtual void PerformTransition(SerializedProperty property)
        {
            SerializedProperty _items = property.FindPropertyRelative("_items");
            SerializedProperty _icon;
            SerializedProperty _iconAsset;
            string guid;
            for (int i = 0; i < _items.arraySize; i++)
            {
                _icon = _items.GetArrayElementAtIndex(i).FindPropertyRelative("_icon");
                _iconAsset = _items.GetArrayElementAtIndex(i).FindPropertyRelative("_iconAsset");
                guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_icon.objectReferenceValue));
                _iconAsset.FindPropertyRelative("m_AssetGUID").stringValue = guid;

                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings.FindAssetEntry(guid) == null)
                {
                    settings.CreateAssetReference(guid);
                }
            }
        }
    }
}