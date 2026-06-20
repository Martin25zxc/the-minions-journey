using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NotificationToastView : MonoBehaviour
{
    [Header("Referencias visuales")]
    [Tooltip("CanvasGroup del toast. Si falta, se intenta buscar en este mismo objeto.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Texto corto para el canal: Mission, Combat, Inventory, etc.")]
    [SerializeField] private TMP_Text channelText;

    [Tooltip("Título opcional del toast.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Mensaje principal del toast.")]
    [SerializeField] private TMP_Text messageText;

    [Header("Comportamiento")]
    [Tooltip("Tiempo del fade de entrada y salida. Dejémoslo simple por ahora.")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.15f;

    [Tooltip("Si está activo, el toast se destruye al terminar. Para pooling futuro se puede apagar.")]
    [SerializeField] private bool destroyAfterHide = true;

    private NotificationData currentNotification;
    private Action<NotificationData> finishedCallback;
    private Coroutine displayRoutine;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Show(NotificationData notification, Action<NotificationData> onFinished)
    {
        if (notification == null)
        {
            Debug.LogWarning($"{nameof(NotificationToastView)} recibió una notificación null.", this);
            return;
        }

        currentNotification = notification;
        finishedCallback = onFinished;

        gameObject.SetActive(true);
        ApplyTexts(notification);

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
        }

        displayRoutine = StartCoroutine(DisplayRoutine(notification));
    }

    private void ApplyTexts(NotificationData notification)
    {
        if (channelText != null)
        {
            channelText.text = notification.Channel.ToString();
        }

        if (titleText != null)
        {
            titleText.gameObject.SetActive(notification.HasTitle);
            titleText.text = notification.Title;
        }

        if (messageText != null)
        {
            messageText.text = notification.Message;
        }
    }

    private IEnumerator DisplayRoutine(NotificationData notification)
    {
        float duration = notification.Duration > 0f ? notification.Duration : 3f;

        yield return FadeTo(1f);
        yield return new WaitForSecondsRealtime(duration);
        yield return FadeTo(0f);

        finishedCallback?.Invoke(notification);
        finishedCallback = null;
        currentNotification = null;
        displayRoutine = null;

        if (destroyAfterHide)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (canvasGroup == null || fadeDuration <= 0f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }

            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void OnDisable()
    {
        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }

        if (currentNotification != null)
        {
            finishedCallback?.Invoke(currentNotification);
        }

        finishedCallback = null;
        currentNotification = null;
    }
}
