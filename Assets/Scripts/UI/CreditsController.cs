using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CreditsController : MonoBehaviour
{
    [SerializeField]
    private Button backButton;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(GoBackToMainMenu);
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(GoBackToMainMenu);
    }

    public void GoBackToMainMenu()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadSceneAsync(mainMenuSceneName);
    }
}
