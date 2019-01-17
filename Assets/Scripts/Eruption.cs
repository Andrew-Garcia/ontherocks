using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eruption : MonoBehaviour 
{
	public float TimeBetweenEruptions;
	public float startDelay;

	public ParticleSystem er1;
	public ParticleSystem er2;

	public Lava lavaScript;

	void Start () 
	{
		
	}
	
	public IEnumerator Erupt()
	{
		yield return new WaitForSeconds(startDelay);

		while (true)
		{
			Debug.Log("start");

			er1.Play();
			er2.Play();
			yield return new WaitForSeconds(2f);
			lavaScript.StartCoroutine(lavaScript.Rise());
			yield return new WaitForSeconds(TimeBetweenEruptions);
		}
	}
}
