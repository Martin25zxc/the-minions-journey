using UnityEngine;

public sealed class WorldSpaceHealthBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform fillRect;
    [SerializeField] RectTransform ghostRect;

    [Tooltip("Asignar HealthBarCanvas. No asignar HealthBarRoot.")]
    [SerializeField] GameObject visualRoot;

    [Header("Visibility")]
    [SerializeField] bool hideWhenEmpty = true;

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

        if (visualRoot == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);

            if (canvas != null)
            {
                visualRoot = canvas.gameObject;
            }
            else
            {
                Debug.LogWarning("[WorldSpaceHealthBar] visualRoot not assigned. Assign HealthBarCanvas if you want to hide the bar.", this);
            }
        }

        _maxWidth = ghostRect.rect.width;

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        float ratio = _health.MaxHealth > 0f
            ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth)
            : 0f;

        fillRect.sizeDelta = new Vector2(ratio * _maxWidth, fillRect.sizeDelta.y);

        if (hideWhenEmpty && visualRoot != null)
        {
            bool shouldShow = _health.CurrentHealth > 0f;

            if (visualRoot.activeSelf != shouldShow)
            {
                visualRoot.SetActive(shouldShow);
            }
        }
    }
}