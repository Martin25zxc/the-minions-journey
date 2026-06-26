using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TMJ_MainMenuController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField]
    private string newGameSceneName = "SC_LevelIntro_01";

    [Header("Main Buttons")]
    [SerializeField]
    private Button newGameButton;

    [SerializeField]
    private Button creditsButton;

    [SerializeField]
    private TMP_Text versionText;

    private bool isLoading;

    private void Awake()
    {
        Time.timeScale = 1f;

        BindButtons();
        RefreshVersionText();
        SetButtonsInteractable(true);
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OpenCredits);
    }

    private void UnbindButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(StartNewGame);
        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(OpenCredits);
    }

    public void StartNewGame()
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(newGameSceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("No se configuró el nombre de la escena para Nueva partida.");
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

    private void RefreshVersionText()
    {
        if (versionText != null)
            versionText.text = $"v{Application.version}";
    }

    public void OpenCredits()
    {
        if (isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded("Credits"))
        {
            Debug.LogError("La escena 'Credits' no existe o no esta agregada en Build Settings.");
            return;
        }

        isLoading = true;
        SetButtonsInteractable(false);
        SceneManager.LoadSceneAsync("Credits");
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (newGameButton != null)
            newGameButton.interactable = interactable;
        if (creditsButton != null)
            creditsButton.interactable = interactable;
    }
}// trigger
