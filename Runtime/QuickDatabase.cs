using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DaBois.Utilities
{
    [System.Serializable]
    public abstract class QuickDatabase
    {
        public abstract QuickDatabaseItem GetItem(int index);
        public abstract QuickDatabaseItem GetItem(int index, bool ordered);
        public abstract void GetOrderedItems(ref List<QuickDatabaseItem> items);
#if UNITY_EDITOR
        [SerializeField]
        protected QuickDatabaseEditorSettings _editorSettings = new QuickDatabaseEditorSettings(10);
#endif
    }
}