using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if QuickDatabaseSettings_Transition || QuickDatabaseSettings_Addressables
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace DaBois.Utilities
{
    [System.Serializable]
    public class QuickDatabaseItem
    {
        [SerializeField]
        [HideInInspector]
        private ushort _id = default;
        [SerializeField]
        [HideInInspector]
        private ushort _order = default;
        [SerializeField]
        private string _name = default;
        [SerializeField]
        [HideInInspector]
        private bool _notValid = false;

        [SerializeField]
#if QuickDatabaseSettings_Transition
        private Sprite _icon = default;
        [SerializeField]
        private AssetReferenceSprite _iconAsset = default;
        //public void GetIcon(System.Action<Sprite> callback) { }
#elif QuickDatabaseSettings_Addressables
        protected AssetReferenceSprite _iconAsset = default;
#else
        private Sprite _icon = default;
#endif
        //private List<AsyncOperationHandle> _ops = new List<AsyncOperationHandle>();

        public ushort Order { get => _order; }
        public string Name { get => _name; }
        public ushort Id { get => _id; }
        public AssetReferenceSprite IconAsset { get => _iconAsset; }

#if QuickDatabaseSettings_Transition || QuickDatabaseSettings_Addressables
        public AsyncOperationHandle GetIcon(System.Action<Sprite> callback)
        {
            if (!_iconAsset.RuntimeKeyIsValid())
            {
                callback.Invoke(null);
                return new AsyncOperationHandle();
            }
            AsyncOperationHandle<Sprite> op = Addressables.LoadAssetAsync<Sprite>(_iconAsset);
            op.Completed += (c) =>
            {
                callback.Invoke(c.Result);
            };
            return op;
        }

        public void ReleaseIcon(AsyncOperationHandle op)
        {
            /*if (_iconAsset.IsValid())
            {
                Debug.Log("Releaasing database icon");
                Addressables.Release(_iconAsset);
            }
            else
            {
                Debug.Log("Cannot Releaasing database icon. Not Valid");
            }*/
            if (op.IsValid())
            {
                Addressables.Release(op);
            }
        }
#else
        public Sprite Icon { get => _icon; }
#endif

    }
}