using UnityEngine;

#if UNITY_EDITOR
using System;
[System.Serializable]
public struct QuickDatabaseEditorSettings
{
    [SerializeField]
    public int ItemsPerPage;
    [SerializeField]
    private string _id;

    public QuickDatabaseEditorSettings(int itemsPerPage)
    {
        ItemsPerPage = itemsPerPage;
        _id = default;
    }

    /// <summary>
    /// Generate guid if empty
    /// </summary>
    /// <returns></returns>
    public Guid TryGenerate()
    {
        if (new Guid(_id) == Guid.Empty)
        {
            _id = Guid.NewGuid().ToString();
        }

        return new Guid(_id);
    }
}
#endif
