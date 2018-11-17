using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFocus : MonoBehaviour 
{
	public List<Transform> focusTargets;
	public Vector3 offset;

	public float smoothTime = 0.5f;
	public float minZoom = 10;
	public float maxZoom = 40;

	public float zoomLimiter;

	Vector3 velocity;
	Camera cam;

	void Start () 
	{
		cam = GetComponent<Camera>();	
	}
	
	void LateUpdate () 
	{
		if (focusTargets.Count == 0) return;

		Move();
		Zoom();
	}

	void Move()
	{
		Vector3 centerPoint = GetCenterPoint();
		Vector3 newPosition = centerPoint + offset;

		transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
	}

	void Zoom()
	{
		float newZoom = Mathf.Lerp(minZoom, maxZoom, GetGreatestDistance() / zoomLimiter);
		cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newZoom, Time.deltaTime);
	}

	Vector3 GetCenterPoint()
	{
		if (focusTargets.Count == 1) return new Vector3(focusTargets[0].position.x, offset.y);
		
		Bounds bounds = new Bounds(focusTargets[0].position, Vector3.zero);

		for (int i = 0; i < focusTargets.Count; i++)
		{
			bounds.Encapsulate(focusTargets[i].position);
		}

		return new Vector3(bounds.center.x, offset.y);
	}

	float GetGreatestDistance()
	{
		Bounds bounds = new Bounds(focusTargets[0].position, Vector3.zero);

		for (int i = 0; i < focusTargets.Count; i++)
		{
			bounds.Encapsulate(focusTargets[i].position);
		}

		return bounds.size.x;
	}

	public void AddFocus(Transform focus)
	{
		focusTargets.Add(focus);
	}

	public void RemoveFocus(Transform focus)
	{
		focusTargets.Remove(focus);
	}
}
