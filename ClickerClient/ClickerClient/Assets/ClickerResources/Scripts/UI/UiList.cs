using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameClient.UI
{
    /// <summary>
    /// Rebuilds list rows from a prefab.
    /// Removes only TItem rows and leaves other objects in root (labels, headers) untouched.
    /// The prefab can be an asset or an inactive template inside root; the template is not removed either.
    /// </summary>
    public static class UiList
    {
        public static List<TItem> Rebuild<TItem, TData>(
            Transform root, TItem prefab, IEnumerable<TData> data, Action<TItem, TData> bind)
            where TItem : Component
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);

                if (child != prefab.transform && child.GetComponent<TItem>() != null)
                    Object.Destroy(child.gameObject);
            }

            var items = new List<TItem>();

            foreach (var item in data)
            {
                var view = Object.Instantiate(prefab, root);
                view.gameObject.SetActive(true); // a scene template is usually disabled
                bind(view, item);
                items.Add(view);
            }

            return items;
        }
    }
}
