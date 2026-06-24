using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldSpaceBillboard : MonoBehaviour
{
    public enum BillboardMode
    {
        MatchCameraRotation,
        LookAtCamera
    }

    [Header("Cámara")]
    [SerializeField, Tooltip("Cámara que debe mirar el indicador. Si queda vacío, usa Camera.main.")]
    private Camera targetCamera;

    [Header("Modo")]
    [SerializeField, Tooltip("MatchCameraRotation suele funcionar bien para Canvas World Space. LookAtCamera apunta físicamente hacia la cámara.")]
    private BillboardMode mode = BillboardMode.MatchCameraRotation;

    [SerializeField, Tooltip("Si el texto queda al revés, activar esta opción para rotarlo 180 grados en Y.")]
    private bool flipY;

    [SerializeField, Tooltip("Si está activo, busca Camera.main si la referencia está vacía.")]
    private bool autoResolveMainCamera = true;

    private void LateUpdate()
    {
        Camera cameraToUse = ResolveCamera();

        if (cameraToUse == null)
        {
            return;
        }

        if (mode == BillboardMode.MatchCameraRotation)
        {
            transform.rotation = cameraToUse.transform.rotation;
        }
        else
        {
            Vector3 direction = transform.position - cameraToUse.transform.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        if (flipY)
        {
            transform.Rotate(0f, 180f, 0f, Space.Self);
        }
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null)
        {
            return targetCamera;
        }

        if (!autoResolveMainCamera)
        {
            return null;
        }

        targetCamera = Camera.main;
        return targetCamera;
    }
}
