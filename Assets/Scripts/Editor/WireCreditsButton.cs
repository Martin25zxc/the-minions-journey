using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class WireCreditsButton
{
    private static bool _done;

    static WireCreditsButton()
    {
        EditorApplication.delayCall += () =>
        {
            if (_done || EditorApplication.isPlayingOrWillChangePlaymode) return;
            _done = true;
            Wire();
        };
    }

    [MenuItem("Tools/Wire Credits Button")]
    public static void Wire()
    {
        var btn = GameObject.Find("CreditsButton");
        if (btn == null) { Debug.LogWarning("CreditsButton not found yet."); return; }

        // Already wired? Check if it has a parent
        if (btn.transform.parent != null && btn.transform.parent.name == "ButtonPanel")
        {
            if (btn.GetComponent<Button>() != null)
            {
                var existingCtrl = GameObject.FindFirstObjectByType<TMJ_MainMenuController>();
                if (existingCtrl != null)
                {
                    var so = new SerializedObject(existingCtrl);
                    var prop = so.FindProperty("creditsButton");
                    if (prop != null && prop.objectReferenceValue != null)
                    {
                        Debug.Log("CreditsButton already wired.");
                        return;
                    }
                }
            }
        }

        var panel = GameObject.Find("ButtonPanel");
        if (panel == null) { Debug.LogError("ButtonPanel not found!"); return; }

        btn.transform.SetParent(panel.transform, false);

        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(160, 30);

        var img = btn.GetComponent<Image>();
        if (img == null) img = btn.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.1f);

        var textGO = btn.transform.Find("Text")?.gameObject;
        if (textGO == null)
        {
            textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(btn.transform, false);
        }
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = "CREDITOS";
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        var controller = GameObject.FindFirstObjectByType<TMJ_MainMenuController>();
        if (controller != null)
        {
            var so = new SerializedObject(controller);
            var prop = so.FindProperty("creditsButton");
            if (prop != null)
            {
                prop.objectReferenceValue = btn.GetComponent<Button>();
                so.ApplyModifiedProperties();
            }
        }

        bool found = false;
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path == "Assets/Scenes/Credits.unity") { found = true; break; }
        if (!found)
        {
            var scenes = new EditorBuildSettingsScene[EditorBuildSettings.scenes.Length + 1];
            System.Array.Copy(EditorBuildSettings.scenes, scenes, EditorBuildSettings.scenes.Length);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene("Assets/Scenes/Credits.unity", true);
            EditorBuildSettings.scenes = scenes;
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Credits button wired successfully!");
    }
}
