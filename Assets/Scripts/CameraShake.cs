using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour {
	float shakeDuration = 0f;
	[SerializeField] float maxShakeDuration;

	[SerializeField] float baseShakeAmount;
	[SerializeField] float bigMultiplier = 2f;
	float shakeAmount = 0f;



	// Update is called once per frame
	void FixedUpdate () {
		if (shakeDuration > 0f) {
			Vector3 shake = Random.insideUnitCircle * shakeAmount;
			transform.position += shake;
			shakeDuration -= Time.deltaTime;
		} else if (shakeDuration < 0f) {
			shakeDuration = 0f;
		}		
	}

	public void shake(bool big) {
		shakeDuration = maxShakeDuration;
		shakeAmount = baseShakeAmount;
		if (big) {
			shakeDuration *= bigMultiplier;
			shakeAmount *= bigMultiplier;
		}
	}



}
