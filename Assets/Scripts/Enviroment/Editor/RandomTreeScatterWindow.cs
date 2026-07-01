using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RandomTreeScatterWindow : EditorWindow
{
    private readonly List<GameObject> treePrefabs = new();

    private Transform parentOverride;
    private float brushRadius = 3f;
    private int treesPerStroke = 1;
    private float minScaleVariation = 0.1f;
    private float maxScaleVariation = 0.1f;
    private float raycastHeight = 100f;
    private int groundMask = ~0;
    private bool alignToSurface = true;
    private bool randomYRotation = true;
    private bool createParentContainer = true;
    private bool paintingEnabled = true;
    private float paintInterval = 0.2f;
    private string containerName = "Random Trees";
    private int seed;
    private bool useRandomSeed = true;
    private Vector3 lastPaintHitPoint;
    private double nextPaintTime;

    [MenuItem("Tools/Environment/Random Tree Scatter")]
    public static void ShowWindow()
    {
        GetWindow<RandomTreeScatterWindow>("Tree Scatter");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Tree Brush", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Paint tree prefabs directly in the Scene View with click or drag. Each instance gets a random Y rotation and a tiny scale variation.", MessageType.Info);

        DrawTreePrefabs();
        DrawBrushSettings();

        GUILayout.Space(8f);

        paintingEnabled = EditorGUILayout.Toggle("Painting Enabled", paintingEnabled);
        EditorGUILayout.LabelField("Usage", "Left click or drag in the Scene View to paint trees.");
    }

    private void DrawTreePrefabs()
    {
        GUILayout.Space(6f);
        GUILayout.Label("Tree Prefabs", EditorStyles.boldLabel);

        for (int index = 0; index < treePrefabs.Count; index++)
        {
            EditorGUILayout.BeginHorizontal();
            treePrefabs[index] = (GameObject)EditorGUILayout.ObjectField($"Tree {index + 1}", treePrefabs[index], typeof(GameObject), false);
            if (GUILayout.Button("-", GUILayout.Width(24f)))
            {
                treePrefabs.RemoveAt(index);
                index--;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Tree Prefab"))
        {
            treePrefabs.Add(null);
        }
    }

    private void DrawBrushSettings()
    {
        GUILayout.Space(6f);
        GUILayout.Label("Brush Settings", EditorStyles.boldLabel);

        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.1f, 10f);
        treesPerStroke = EditorGUILayout.IntSlider("Trees Per Stroke", treesPerStroke, 1, 20);
        paintInterval = EditorGUILayout.Slider("Paint Interval", paintInterval, 0.02f, 1f);
        groundMask = EditorGUILayout.MaskField("Ground Mask", groundMask, UnityEditorInternal.InternalEditorUtility.layers);
        raycastHeight = EditorGUILayout.FloatField("Raycast Height", raycastHeight);
        alignToSurface = EditorGUILayout.Toggle("Align To Surface", alignToSurface);
        randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", randomYRotation);

        GUILayout.Space(4f);
        GUILayout.Label("Scale Variation", EditorStyles.boldLabel);
        minScaleVariation = EditorGUILayout.Slider("Min Variation", minScaleVariation, 0f, 5f);
        maxScaleVariation = EditorGUILayout.Slider("Max Variation", maxScaleVariation, 0f, 5f);

        GUILayout.Space(4f);
        useRandomSeed = EditorGUILayout.Toggle("Use Random Seed", useRandomSeed);
        EditorGUI.BeginDisabledGroup(useRandomSeed);
        seed = EditorGUILayout.IntField("Seed", seed);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(4f);
        parentOverride = (Transform)EditorGUILayout.ObjectField("Parent", parentOverride, typeof(Transform), true);
        createParentContainer = EditorGUILayout.Toggle("Create Container", createParentContainer);

        if (createParentContainer)
        {
            containerName = EditorGUILayout.TextField("Container Name", containerName);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!paintingEnabled)
        {
            return;
        }

        Event currentEvent = Event.current;
        if (currentEvent == null || currentEvent.alt)
        {
            return;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        if (!TryRaycastGround(ray, out RaycastHit hit))
        {
            return;
        }

        lastPaintHitPoint = hit.point;

        Handles.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Handles.DrawWireDisc(hit.point, hit.normal, brushRadius);

        if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && currentEvent.button == 0)
        {
            if (EditorApplication.timeSinceStartup < nextPaintTime)
            {
                currentEvent.Use();
                return;
            }

            nextPaintTime = EditorApplication.timeSinceStartup + paintInterval;
            PaintAt(hit);
            currentEvent.Use();
            SceneView.RepaintAll();
        }
    }

    private void PaintAt(RaycastHit hit)
    {
        List<GameObject> validPrefabs = GetValidPrefabs();
        if (validPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Tree Brush", "Add at least one tree prefab.", "OK");
            return;
        }

        if (minScaleVariation > maxScaleVariation)
        {
            (minScaleVariation, maxScaleVariation) = (maxScaleVariation, minScaleVariation);
        }

        Transform parent = GetOrCreateParent(hit.point);
        int chosenSeed = useRandomSeed ? System.Environment.TickCount : seed;
        System.Random random = new(chosenSeed ^ hit.point.GetHashCode() ^ Time.frameCount);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        for (int index = 0; index < treesPerStroke; index++)
        {
            GameObject prefab = validPrefabs[random.Next(validPrefabs.Count)];
            Vector3 position = GetBrushPosition(hit, random);
            Quaternion rotation = GetRandomRotation(random, position, hit.normal);
            float scaleFactor = GetRandomScaleFactor(random);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Scatter Tree");

            if (parent != null)
            {
                Undo.SetTransformParent(instance.transform, parent, "Scatter Tree");
            }

            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = instance.transform.localScale * scaleFactor;
            EditorUtility.SetDirty(instance);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    private Vector3 GetBrushPosition(RaycastHit hit, System.Random random)
    {
        float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
        float radius = brushRadius * Mathf.Sqrt((float)random.NextDouble());

        Vector2 offset = new(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector3 spawnPoint = hit.point + new Vector3(offset.x * radius, 0f, offset.y * radius);

        Vector3 rayOrigin = spawnPoint + Vector3.up * raycastHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit offsetHit, raycastHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return offsetHit.point;
        }

        return spawnPoint;
    }

    private List<GameObject> GetValidPrefabs()
    {
        List<GameObject> validPrefabs = new();

        foreach (GameObject prefab in treePrefabs)
        {
            if (prefab != null)
            {
                validPrefabs.Add(prefab);
            }
        }

        return validPrefabs;
    }

    private Transform GetOrCreateParent(Vector3 position)
    {
        if (parentOverride != null)
        {
            return parentOverride;
        }

        if (!createParentContainer)
        {
            return null;
        }

        GameObject container = new(containerName);
        Undo.RegisterCreatedObjectUndo(container, "Create Tree Container");
        container.transform.position = position;
        return container.transform;
    }

    private bool TryRaycastGround(Ray ray, out RaycastHit hit)
    {
        return Physics.Raycast(ray, out hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore);
    }

    private Quaternion GetRandomRotation(System.Random random, Vector3 position, Vector3 surfaceNormal)
    {
        float yRotation = randomYRotation ? Mathf.Lerp(0f, 360f, (float)random.NextDouble()) : 0f;

        if (alignToSurface)
        {
            Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            return surfaceRotation * Quaternion.Euler(0f, yRotation, 0f);
        }

        return Quaternion.Euler(0f, yRotation, 0f);
    }

    private float GetRandomScaleFactor(System.Random random)
    {
        float minFactor = Mathf.Max(0.01f, 1f - Mathf.Abs(minScaleVariation));
        float maxFactor = Mathf.Max(minFactor, 1f + Mathf.Abs(maxScaleVariation));
        return Mathf.Lerp(minFactor, maxFactor, (float)random.NextDouble());
    }
}