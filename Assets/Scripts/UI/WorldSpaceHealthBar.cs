using UnityEngine;

public sealed class WorldSpaceHealthBar : MonoBehaviour
{
    [SerializeField] RectTransform fillRect;
    [SerializeField] RectTransform ghostRect;

    TopDownHealth _health;
    float _maxWidth;

    void Start()
    {
        _health = GetComponentInParent<TopDownHealth>();

        if (_health == null)
        {
            Debug.LogError("[WorldSpaceHealthBar] No TopDownHealth found in parent hierarchy.", this);
            enabled = false;
            return;
        }

        if (fillRect == null || ghostRect == null)
        {
            Debug.LogError("[WorldSpaceHealthBar] fillRect or ghostRect not assigned.", this);
            enabled = false;
            return;
        }

        // Anchor + pivot izquierda para que el ancho crezca/encoge de derecha a izquierda
       // fillRect.anchorMin = new Vector2(0f, 0f);
        //fillRect.anchorMax = new Vector2(0f, 1f);
        //fillRect.pivot     = new Vector2(0f, 0.5f);
        //fillRect.offsetMin = Vector2.zero;
        //fillRect.offsetMax = Vector2.zero;

        _maxWidth = ghostRect.rect.width;
    }

    void Update()
    {
        float ratio = _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 0f;
        fillRect.sizeDelta = new Vector2(ratio * _maxWidth, fillRect.sizeDelta.y);
    }
}
