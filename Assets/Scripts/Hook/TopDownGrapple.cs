using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TopDownPlayerController))]
public sealed class TopDownGrapple : MonoBehaviour
{
    [SerializeField] GameObject hookPrefab;
    [SerializeField, Min(1f)] float pullSpeed = 16f;
    [SerializeField, Min(0.1f)] float arrivalThreshold = 1.5f;
    [SerializeField, Min(0f)] float hookSpawnDistance = 2.2f;

    [Header("Rope Visual")]
    [SerializeField, Min(0.005f)] float ropeWidth = 0.04f;
    [SerializeField] Color ropeColor = new Color(0.85f, 0.7f, 0.4f, 1f);
    [SerializeField, Min(0f)] float ropeOriginHeightOffset = 0.5f;

    Rigidbody body;
    TopDownPlayerController controller;
    GrapplingHookProjectile activeHook;
    LineRenderer rope;
    Vector3 hookTarget;
    bool pulling;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        controller = GetComponent<TopDownPlayerController>();
        rope = BuildRopeRenderer();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame && activeHook == null && !pulling)
            FireHook();

        UpdateRopeVisual();
    }

    void FixedUpdate()
    {
        if (!pulling) return;

        Vector3 toTarget = hookTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= arrivalThreshold)
        {
            CancelGrapple();
            return;
        }

        body.linearVelocity = toTarget.normalized * pullSpeed;
    }

    void FireHook()
    {
        if (hookPrefab == null)
        {
            Debug.LogWarning("TopDownGrapple: hookPrefab not assigned.", this);
            return;
        }

        Vector3 dir = controller.AimDirection;
        if (dir.sqrMagnitude < 0.001f) return;

        Vector3 spawnPos = transform.position + dir * hookSpawnDistance + Vector3.up * ropeOriginHeightOffset;
        GameObject hookObj = Instantiate(hookPrefab, spawnPos, Quaternion.LookRotation(dir));

        activeHook = hookObj.GetComponent<GrapplingHookProjectile>();
        if (activeHook == null)
        {
            Destroy(hookObj);
            return;
        }

        activeHook.OnHookLanded += HandleHookLanded;
        activeHook.OnHookMissed += CancelGrapple;
        activeHook.Launch(dir);
    }

    void HandleHookLanded(Vector3 hitPoint)
    {
        hookTarget = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
        pulling = true;
        controller.enabled = false;
    }

    void CancelGrapple()
    {
        pulling = false;
        controller.enabled = true;

        if (activeHook != null)
        {
            activeHook.OnHookLanded -= HandleHookLanded;
            activeHook.OnHookMissed -= CancelGrapple;
            Destroy(activeHook.gameObject);
            activeHook = null;
        }

        rope.enabled = false;
    }

    void UpdateRopeVisual()
    {
        if (activeHook == null)
        {
            rope.enabled = false;
            return;
        }

        rope.enabled = true;
        rope.SetPosition(0, transform.position + Vector3.up * ropeOriginHeightOffset);
        rope.SetPosition(1, activeHook.transform.position);
    }

    LineRenderer BuildRopeRenderer()
    {
        GameObject ropeObj = new GameObject("GrappleRope");
        ropeObj.transform.SetParent(transform, false);

        LineRenderer lr = ropeObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = ropeWidth;
        lr.endWidth = ropeWidth * 0.5f;
        lr.startColor = ropeColor;
        lr.endColor = ropeColor;
        lr.alignment = LineAlignment.View;
        lr.numCapVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.sharedMaterial = BuildRopeMaterial(ropeColor);
        lr.enabled = false;

        return lr;
    }

    static Material BuildRopeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");

        var mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        return mat;
    }

    void OnDrawGizmos()
    {
        if (pulling)
        {
            // Play mode: arrival zone at hook landing point
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.2f);
            Gizmos.DrawSphere(hookTarget, arrivalThreshold);
            Gizmos.color = new Color(0f, 1f, 0.4f, 1f);
            Gizmos.DrawWireSphere(hookTarget, arrivalThreshold);
            float c = arrivalThreshold * 0.3f;
            Gizmos.DrawLine(hookTarget - Vector3.right * c, hookTarget + Vector3.right * c);
            Gizmos.DrawLine(hookTarget - Vector3.forward * c, hookTarget + Vector3.forward * c);
        }
        else
        {
            // Editor preview: spawn point indicator
            Vector3 preview = transform.position + transform.forward * hookSpawnDistance + Vector3.up * ropeOriginHeightOffset;
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.15f);
            Gizmos.DrawSphere(preview, 0.15f);
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            Gizmos.DrawWireSphere(preview, 0.15f);
        }
    }
}
