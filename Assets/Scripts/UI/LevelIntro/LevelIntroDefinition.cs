using UnityEngine;

[CreateAssetMenu(menuName = "Game/Flow/Level Intro Definition")]
public sealed class LevelIntroDefinition : ScriptableObject
{
    [Header("Scene")]
    [SerializeField]
    private string gameplaySceneName = "";

    [Header("Identity")]
    [SerializeField]
    private string chapterLabel = "";

    [SerializeField]
    private string missionTitle = "";

    [SerializeField]
    private string locationName = "";

    [Header("Content")]
    [TextArea(3, 8)]
    [SerializeField]
    private string description;

    [SerializeField]
    private Sprite levelImage;

    [TextArea(2, 4)]
    [SerializeField]
    private string loadingTip;

    [Header("Behaviour")]
    [SerializeField]
    private bool requirePlayerConfirmation = true;

    public string GameplaySceneName => gameplaySceneName;
    public string ChapterLabel => chapterLabel;
    public string MissionTitle => missionTitle;
    public string LocationName => locationName;
    public string Description => description;
    public Sprite LevelImage => levelImage;
    public string LoadingTip => loadingTip;
    public bool RequirePlayerConfirmation => requirePlayerConfirmation;
}