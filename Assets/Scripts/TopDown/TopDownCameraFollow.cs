using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class TopDownCameraFollow : MonoBehaviour
{
	[SerializeField]
	Vector3 offset = new Vector3(0f, 12f, -10f);

	[SerializeField, Min(0.01f)]
	float rotationSensitivity = 0.25f;

	[SerializeField, Min(0.01f)]
	float zoomSensitivity = 1.0f;

	[SerializeField, Min(0.1f)]
	float minZoomScale = 0.6f;

	[SerializeField, Min(0.1f)]
	float maxZoomScale = 1.6f;

	[SerializeField, Min(0.01f)]
	float followSharpness = 12f;

	TopDownPlayerController target;
	float yaw;
	float zoomScale = 1f;

	void Awake()
	{
		target = FindFirstObjectByType<TopDownPlayerController>();
		yaw = transform.eulerAngles.y;
	}

	void LateUpdate()
	{
		if (target == null)
		{
			target = FindFirstObjectByType<TopDownPlayerController>();
			if (target == null)
			{
				return;
			}
		}

		ReadOrbitInput();
		ReadZoomInput();

		Vector3 targetPosition = target.transform.position;
		Quaternion orbitRotation = Quaternion.Euler(0f, yaw, 0f);
		Vector3 desiredPosition = targetPosition + orbitRotation * (offset * zoomScale);
		float smoothing = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

		transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothing);

		Vector3 lookTarget = targetPosition + Vector3.up * 1f;
		Vector3 lookDirection = lookTarget - transform.position;
		if (lookDirection.sqrMagnitude > 0.001f)
		{
			Vector3 desiredForward = lookDirection.normalized;
			Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, smoothing);
		}
	}

	void ReadOrbitInput()
	{
		Mouse mouse = Mouse.current;
		if (mouse?.middleButton.isPressed != true)
		{
			return;
		}

		Vector2 delta = mouse.delta.ReadValue();
		yaw += delta.x * rotationSensitivity;
	}

	void ReadZoomInput()
	{
		Mouse mouse = Mouse.current;
		if (mouse == null)
		{
			return;
		}

		float scrollDelta = mouse.scroll.ReadValue().y;
		if (Mathf.Abs(scrollDelta) < 0.001f)
		{
			return;
		}

		zoomScale = Mathf.Clamp(zoomScale - scrollDelta * zoomSensitivity, minZoomScale, maxZoomScale);
	}
}
