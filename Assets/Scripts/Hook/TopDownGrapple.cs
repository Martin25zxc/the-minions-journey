using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TopDownPlayerController))]
public sealed class TopDownGrapple : MonoBehaviour, ISkillBehaviour

{
    public string SkillID => "hook";

    [Header("Input")]
    [SerializeField]
    private bool readInputDirectly = true;

    [SerializeField]
    private bool pressQAgainCancels = true;

    [Header("Hook")]
    [SerializeField]
    private GameObject hookPrefab;

    [SerializeField, Min(1f)]
    private float pullSpeed = 16f;

    [SerializeField, Min(0.1f)]
    private float arrivalThreshold = 1.5f;

    [SerializeField, Min(0f)]
    private float hookSpawnDistance = 2.2f;

    [Header("Rope Visual")]
    [SerializeField, Min(0.005f)]
    private float ropeWidth = 0.04f;

    [SerializeField]
    private Color ropeColor = new Color(0.85f, 0.7f, 0.4f, 1f);

    [SerializeField, Min(0f)]
    private float ropeOriginHeightOffset = 0.5f;

    private Rigidbody body;
    private TopDownPlayerController controller;
    private GrapplingHookProjectile activeHook;
    private LineRenderer rope;

    private Vector3 hookTarget;
    private bool pulling;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        controller = GetComponent<TopDownPlayerController>();
        rope = BuildRopeRenderer();
    }

    private void Update()
    {
        if (readInputDirectly)
        {
            ReadDirectInput();
        }

        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        if (!pulling)
        {
            return;
        }

        Vector3 toTarget = hookTarget - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= arrivalThreshold)
        {
            CancelGrapple();
            return;
        }

        Vector3 horizontalVelocity = toTarget.normalized * pullSpeed;

        body.linearVelocity = new Vector3(
            horizontalVelocity.x,
            body.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    private void ReadDirectInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (!keyboard.qKey.wasPressedThisFrame)
        {
            return;
        }

        if (activeHook != null || pulling)
        {
            if (pressQAgainCancels)
            {
                CancelGrapple();
            }

            return;
        }

        FireHook();
    }

    private void FireHook()
    {
        if (activeHook != null || pulling)
        {
            return;
        }

        if (hookPrefab == null)
        {
            Debug.LogWarning("TopDownGrapple: hookPrefab not assigned.", this);
            return;
        }

        if (!TryGetPlanarAimDirection(out Vector3 direction))
        {
            return;
        }

        Vector3 spawnPosition = transform.position
                              + direction * hookSpawnDistance
                              + Vector3.up * ropeOriginHeightOffset;

        GameObject hookObject = Instantiate(
            hookPrefab,
            spawnPosition,
            Quaternion.LookRotation(direction, Vector3.up)
        );

        activeHook = hookObject.GetComponent<GrapplingHookProjectile>();
        if (activeHook == null)
        {
            Debug.LogWarning("TopDownGrapple: hookPrefab does not have GrapplingHookProjectile.", hookObject);
            Destroy(hookObject);
            return;
        }

        activeHook.OnHookLanded += HandleHookLanded;
        activeHook.OnHookMissed += CancelGrapple;

        activeHook.Launch(direction, transform);
    }

    public void Execute()
    {
        FireHook();
        Debug.Log("¡Lanzando el gancho! (Ejecutando Grapple)");
    }

    private bool TryGetPlanarAimDirection(out Vector3 direction)
    {
        direction = controller != null ? controller.AimDirection : transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();
        return true;
    }

    private void HandleHookLanded(Vector3 hitPoint)
    {
        if (activeHook == null)
        {
            return;
        }

        hookTarget = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
        pulling = true;

        if (controller != null)
        {
            controller.enabled = false;
        }

        Vector3 velocity = body.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        body.linearVelocity = velocity;
    }

    private void CancelGrapple()
    {
        pulling = false;

        if (controller != null)
        {
            controller.enabled = true;
        }

        if (body != null)
        {
            Vector3 velocity = body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            body.linearVelocity = velocity;
        }

        if (activeHook != null)
        {
            activeHook.OnHookLanded -= HandleHookLanded;
            activeHook.OnHookMissed -= CancelGrapple;
            activeHook.CancelSilently();
            activeHook = null;
        }

        if (rope != null)
        {
            rope.enabled = false;
        }
    }

    private void UpdateRopeVisual()
    {
        if (rope == null)
        {
            return;
        }

        if (activeHook == null)
        {
            rope.enabled = false;
            return;
        }

        rope.enabled = true;
        rope.SetPosition(0, transform.position + Vector3.up * ropeOriginHeightOffset);
        rope.SetPosition(1, activeHook.transform.position);
    }

    private LineRenderer BuildRopeRenderer()
    {
        GameObject ropeObject = new GameObject("GrappleRope");
        ropeObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = ropeObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth * 0.5f;
        lineRenderer.startColor = ropeColor;
        lineRenderer.endColor = ropeColor;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.numCapVertices = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sharedMaterial = BuildRopeMaterial(ropeColor);
        lineRenderer.enabled = false;

        return lineRenderer;
    }

    private static Material BuildRopeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color");

        Material material = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    private void OnDisable()
    {
        CancelGrapple();
    }

    private void OnDrawGizmos()
    {
        if (pulling)
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.2f);
            Gizmos.DrawSphere(hookTarget, arrivalThreshold);

            Gizmos.color = new Color(0f, 1f, 0.4f, 1f);
            Gizmos.DrawWireSphere(hookTarget, arrivalThreshold);

            float crossSize = arrivalThreshold * 0.3f;
            Gizmos.DrawLine(hookTarget - Vector3.right * crossSize, hookTarget + Vector3.right * crossSize);
            Gizmos.DrawLine(hookTarget - Vector3.forward * crossSize, hookTarget + Vector3.forward * crossSize);
        }
        else
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();

            Vector3 preview = transform.position
                            + forward * hookSpawnDistance
                            + Vector3.up * ropeOriginHeightOffset;

            Gizmos.color = new Color(1f, 0.9f, 0f, 0.15f);
            Gizmos.DrawSphere(preview, 0.15f);

            Gizmos.color = new Color(1f, 0.9f, 0f, 0.9f);
            Gizmos.DrawWireSphere(preview, 0.15f);
        }
    }
}