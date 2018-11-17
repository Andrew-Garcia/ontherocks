using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyObject : MonoBehaviour 
{
	public GameObject objectToDestroy;
	RockScript attachedRock;
	
	void Start () 
	{
		attachedRock = GetComponent<RockScript>();
	}
	
	public void DestoryObject() 
	{
		Transform parent = objectToDestroy.transform.parent;
		Transform child = objectToDestroy.transform.GetChild(0);

		child.parent = parent;
		Destroy(objectToDestroy);
	}
}
