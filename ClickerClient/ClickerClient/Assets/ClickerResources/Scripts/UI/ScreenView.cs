using UnityEngine;

namespace GameClient.UI
{
    /// <summary>A screen or panel that can be shown and hidden.</summary>
    public abstract class ScreenView : MonoBehaviour
    {
        public virtual void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
