using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class TMJ_MainMenuController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField]
    private string newGameSceneName = "Nivel-1";

    [Header("Main Buttons")]
    [SerializeField]
    private Button newGameButton;

    [SerializeField]
    private TMP_Text versionText;

    private bool isLoading;

    private void Awake()
    {
        BindButtons();
        RefreshVersionText();
    }

    private void Start()
    {
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);
    }

    private void UnbindButtons()
    {
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(StartNewGame);
    }

    private void StartNewGame()
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

    private void SetButtonsInteractable(bool interactable)
    {
        if (newGameButton != null)
            newGameButton.interactable = interactable;
    }

}