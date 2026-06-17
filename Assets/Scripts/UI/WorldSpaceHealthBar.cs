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



        _maxWidth = ghostRect.rect.width;
    }

    void Update()
    {
        float ratio = _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 0f;
        fillRect.sizeDelta = new Vector2(ratio * _maxWidth, fillRect.sizeDelta.y);
    }
}
