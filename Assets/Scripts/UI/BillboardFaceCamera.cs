using UnityEngine;

/// <summary>
/// Keeps this transform's rotation aligned with the main camera each frame.
/// Attach to any world-space UI root that should always face the player's screen.
/// </summary>
public sealed class BillboardFaceCamera : MonoBehaviour
{
    Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            return;
        }

        // LookRotation toward camera: canvas +Z points away from cam so its face is visible to cam
        transform.rotation = Quaternion.LookRotation(
            transform.position - _cam.transform.position,
            Vector3.up
        );
    }
}
