using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateLava : MonoBehaviour 
{
	public float fps;
	public Texture[] lava;

	Renderer rend;

	void Start () 
	{
		rend = GetComponent<Renderer>();
		StartCoroutine(Animate());	
	}

	IEnumerator Animate()
	{
		while (true)
		{	
			for (int frame = 0; frame < 16; frame++)
			{
				rend.material.SetTexture("_MainTex", lava[frame]);
				yield return new WaitForSeconds(1 / fps);
			}
		}
	}
}
