using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FinishSceneController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField]
    private string menuSceneName = "MainMenu";

    [Header("Main Buttons")]
    [SerializeField]
    private Button returnButton;

    private bool isLoading;

    private void Awake()
    {
        Time.timeScale = 1f;

        BindButtons();
        SetButtonsInteractable(true);
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (returnButton != null)
            returnButton.onClick.AddListener(StartNewGame);
    }

    private void UnbindButtons()
    {
        if (returnButton != null)
            returnButton.onClick.RemoveListener(StartNewGame);
    }

    public void StartNewGame()
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(menuSceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("No se configuró el nombre de la escena.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"La escena '{sceneName}' no existe o no está agregada en Build Settings.");
            yield break;
        }

        isLoading = true;
        SetButtonsInteractable(false);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        while (loadOperation != null && !loadOperation.isDone)
            yield return null;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (returnButton != null)
            returnButton.interactable = interactable;
    }
}
