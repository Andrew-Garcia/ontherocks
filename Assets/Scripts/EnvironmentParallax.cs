using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentParallax : MonoBehaviour
{
	public float scrollSpeed;

	Transform camTransform;

	float lastCamX;

	void Start()
	{
		camTransform = Camera.main.transform;
	}

	void LateUpdate()
	{
		float deltaX = camTransform.position.x - lastCamX;
		transform.position += Vector3.right * (deltaX * scrollSpeed);
		lastCamX = camTransform.position.x;
	}
}
