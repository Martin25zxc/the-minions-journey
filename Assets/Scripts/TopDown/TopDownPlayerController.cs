using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class TopDownPlayerController : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    float moveSpeed = 6f;

    [SerializeField]
    Camera gameplayCamera;

    [SerializeField, Min(0.001f)]
    float aimDeadZone = 0.001f;

    [SerializeField, Min(0.25f)]
    float facingIndicatorLength = 1.5f;

    [SerializeField, Range(0.01f, 0.2f)]
    float facingIndicatorWidth = 0.05f;

    [SerializeField]
    Color facingIndicatorColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    Rigidbody body;
    LineRenderer facingIndicator;
    Vector2 moveInput;
    Vector3 aimDirection = Vector3.forward;

    public Vector3 AimDirection => aimDirection;

    void Awake()
    {
        body = GetComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        facingIndicator = CreateFacingIndicator();

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    void Update()
    {
        moveInput = ReadMovementInput();
        aimDirection = ReadAimDirection();
    }

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);
        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        body.MovePosition(body.position + movement * moveSpeed * Time.fixedDeltaTime);

        if (aimDirection.sqrMagnitude > aimDeadZone)
        {
            body.MoveRotation(Quaternion.LookRotation(aimDirection, Vector3.up));
        }
    }

    LineRenderer CreateFacingIndicator()
    {
        Transform indicatorRoot = transform.Find("FacingIndicator");
        GameObject indicatorObject;

        if (indicatorRoot == null)
        {
            indicatorObject = new GameObject("FacingIndicator");
            indicatorObject.transform.SetParent(transform, false);
            indicatorObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            indicatorObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            indicatorObject = indicatorRoot.gameObject;
        }

        LineRenderer indicator = indicatorObject.GetComponent<LineRenderer>();
        if (indicator == null)
        {
            indicator = indicatorObject.AddComponent<LineRenderer>();
        }

        indicator.useWorldSpace = false;
        indicator.positionCount = 2;
        indicator.startWidth = facingIndicatorWidth;
        indicator.endWidth = facingIndicatorWidth * 0.8f;
        indicator.startColor = facingIndicatorColor;
        indicator.endColor = facingIndicatorColor;
        indicator.alignment = LineAlignment.View;
        indicator.numCapVertices = 4;
        indicator.numCornerVertices = 4;
        indicator.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        indicator.receiveShadows = false;
        indicator.sharedMaterial = CreateLineMaterial();
        indicator.SetPosition(0, Vector3.zero);
        indicator.SetPosition(1, Vector3.forward * facingIndicatorLength);

        return indicator;
    }

    static Material lineMaterial;

    static Material CreateLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        Shader shader = Shader.Find("TopDown/UnlitVertexColor");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return lineMaterial;
    }

    Vector2 ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            y += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    Vector3 ReadAimDirection()
    {
        Camera activeCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        Mouse mouse = Mouse.current;
        if (activeCamera == null || mouse == null)
        {
            return transform.forward;
        }

        Ray ray = activeCamera.ScreenPointToRay(mouse.position.ReadValue());
        Plane aimPlane = new Plane(Vector3.up, transform.position);

        if (!aimPlane.Raycast(ray, out float enter))
        {
            return transform.forward;
        }

        Vector3 aimPoint = ray.GetPoint(enter);
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude > aimDeadZone ? direction.normalized : transform.forward;
    }
}
