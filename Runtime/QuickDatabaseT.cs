using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DaBois.Utilities
{
    public abstract class QuickDatabaseT<T> : QuickDatabase where T : QuickDatabaseItem
    {
        [SerializeField]
        protected T[] _items = default;
        private Dictionary<int, ushort> _itemsOrdered = new Dictionary<int, ushort>();
        private Dictionary<T, ushort> _itemsId = new Dictionary<T, ushort>();

        [System.NonSerialized]
        private bool _init;

        public T[] Items { get { BaseInit(); return _items; } }

        public void BaseInit()
        {
            if (_init)
            {
                return;
            }
            _init = true;

            for(ushort i = 0; i < _items.Length; i++)
            {
                _itemsOrdered.Add(_items[i].Order, i);
                _itemsId.Add(_items[i], i);
            }

            Init();
        }

        protected virtual void Init()
        {

        }

        public override QuickDatabaseItem GetItem(int index)
        {
            BaseInit();
            index--;

            if(index < 0 || index >= _items.Length)
            {
                return null;
            }
            else
            {
                return _items[index];
            }
        }

        public override QuickDatabaseItem GetItem(int index, bool ordered)
        {
            BaseInit();
            index--;
            

            if (index < 0 || index > _items.Length)
            {
                return null;
            }

            if (!ordered)
            {
                return GetItem(index + 1);
            }
            else
            {
                return _items[_itemsOrdered[index]];
            }
        }

        public override void GetOrderedItems(ref List<QuickDatabaseItem> items)
        {
            BaseInit();

            items.Clear();
            for(int i = 0; i < _items.Length; i++)
            {
                items.Add(_items[i]);
            }

            items.Sort(ItemsOrderer);
        }

        public virtual void GetOrderedItems(ref List<T> items)
        {
            BaseInit();

            items.Clear();
            for(int i = 0; i < _items.Length; i++)
            {
                items.Add(_items[i]);
            }

            items.Sort(ItemsOrderer);
        }
        
        public virtual void GetItems(ref List<T> items)
        {
            BaseInit();
            items.AddRange(_items);
        }

        private int ItemsOrderer(QuickDatabaseItem x, QuickDatabaseItem y)
        {
            return x.Order.CompareTo(y.Order);
        }

        public virtual int GetItemId(T item)
        {
            return _itemsId[item];
        }
    }
}