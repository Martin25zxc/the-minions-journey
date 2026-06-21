using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LevelIntroController : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField]
    private LevelIntroDefinition introDefinition;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text chapterLabelText;

    [SerializeField]
    private TMP_Text missionTitleText;

    [SerializeField]
    private TMP_Text locationNameText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private TMP_Text loadingTipText;

    [SerializeField]
    private TMP_Text loadingStatusText;

    [Header("Image")]
    [SerializeField]
    private Image levelImage;

    [Header("Button")]
    [SerializeField]
    private Button beginButton;

    [SerializeField]
    private TMP_Text beginButtonLabel;

    [Header("Button Text")]
    [SerializeField]
    private string loadingButtonText = "Cargando...";

    [SerializeField]
    private string readyButtonText = "Comenzar";

    [SerializeField]
    private string enteringButtonText = "Entrando...";

    private AsyncOperation loadOperation;
    private bool isReadyToBegin;
    private bool isActivatingScene;

    private void Awake()
    {
        Time.timeScale = 1f;

        BindButtons();
        ApplyDefinitionToUI();
        SetBeginButtonInteractable(false, loadingButtonText);
    }

    private void Start()
    {
        StartCoroutine(LoadGameplaySceneRoutine());
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (beginButton != null)
            beginButton.onClick.AddListener(BeginLevel);
    }

    private void UnbindButtons()
    {
        if (beginButton != null)
            beginButton.onClick.RemoveListener(BeginLevel);
    }

    private void ApplyDefinitionToUI()
    {
        if (introDefinition == null)
        {
            Debug.LogError($"{nameof(LevelIntroController)} no tiene asignado un {nameof(LevelIntroDefinition)}.");
            return;
        }

        SetText(chapterLabelText, introDefinition.ChapterLabel);
        SetText(missionTitleText, introDefinition.MissionTitle);
        SetText(locationNameText, "-"+introDefinition.LocationName+"-");
        SetText(descriptionText, introDefinition.Description);
        SetText(loadingTipText, introDefinition.LoadingTip);
        SetText(loadingStatusText, "Cargando...");

        if (levelImage != null)
        {
            levelImage.sprite = introDefinition.LevelImage;
            levelImage.enabled = introDefinition.LevelImage != null;
        }
    }

    private IEnumerator LoadGameplaySceneRoutine()
    {
        if (introDefinition == null)
        {
            SetText(loadingStatusText, "Error: falta LevelIntroDefinition.");
            yield break;
        }

        string sceneName = introDefinition.GameplaySceneName;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("No se configuró la scene jugable en LevelIntroDefinition.");
            SetText(loadingStatusText, "Error: falta configurar la escena.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"La escena '{sceneName}' no existe o no está agregada en Build Settings.");
            SetText(loadingStatusText, $"Error: la escena '{sceneName}' no está en Build Settings.");
            yield break;
        }

        loadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            Debug.LogError($"No se pudo iniciar la carga de la escena '{sceneName}'.");
            SetText(loadingStatusText, "Error al iniciar la carga.");
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        while (loadOperation.progress < 0.9f)
        {
            float normalizedProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            int percent = Mathf.RoundToInt(normalizedProgress * 100f);

            SetText(loadingStatusText, $"Cargando... {percent}%");

            yield return null;
        }

        isReadyToBegin = true;
        //SetText(loadingStatusText, "Listo.");
        SetText(loadingStatusText, string.Empty);
        if (introDefinition.RequirePlayerConfirmation)
        {
            SetBeginButtonInteractable(true, readyButtonText);
        }
        else
        {
            ActivateLoadedScene();
        }
    }

    public void BeginLevel()
    {
        if (!isReadyToBegin)
            return;

        if (isActivatingScene)
            return;

        ActivateLoadedScene();
    }

    private void ActivateLoadedScene()
    {
        if (loadOperation == null)
        {
            Debug.LogError("No hay una operación de carga activa para activar.");
            return;
        }

        isActivatingScene = true;

        SetBeginButtonInteractable(false, enteringButtonText);
        SetText(loadingStatusText, "Entrando...");

        loadOperation.allowSceneActivation = true;
    }

    private void SetBeginButtonInteractable(bool interactable, string label)
    {
        if (beginButton != null)
            beginButton.interactable = interactable;

        SetText(beginButtonLabel, label);
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}