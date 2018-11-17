using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour 
{
	public float startDelay;
	public float transitionTime;
	public float upTime;
	public float downTime;
	public float blockOffset;

	float boxSize;

	public LayerMask groundLayerMask;

	private void Awake()
	{
		boxSize = GetComponent<BoxCollider2D>().size.y / 2;
	}

	float FindLevel()
	{
		List<Vector3> available = new List<Vector3>();
		float highestLevel = -100, secondLevel = -100; 
		foreach (GameObject rockObject in GameObject.FindGameObjectsWithTag("Rock"))
		{
			RockScript rock = rockObject.GetComponent<RockScript>();
			if (!rock)
				continue;
			if (rock.currentState != RockScript.state.FIXED)
				continue;
			Vector2 rockTop = rock.getTop();
			RaycastHit2D hit = Physics2D.Raycast(rockTop, Vector2.up, 2.5f, groundLayerMask);
			if (!hit.collider)
			{
				if (rock.transform.position.y > highestLevel)
				{
					highestLevel = rock.transform.position.y;
				}
				else if (rock.transform.position.y == highestLevel)
				{
					secondLevel = rock.transform.position.y;
				}
				else if (rock.transform.position.y >= secondLevel)
				{
					secondLevel = rock.transform.position.y;
				}
			}
		}
		return secondLevel;
	}

	void Start () 
	{
		//StartCoroutine(Rise());
	}

	public IEnumerator Rise()
	{
		yield return new WaitForSeconds(startDelay);

		while (true)
		{
			Vector2 startPos = transform.position, endPos = new Vector2(transform.position.x, (FindLevel() - blockOffset) - boxSize);
			float t = 0;
			while (t < transitionTime)
			{
				t += Time.deltaTime / transitionTime;
				transform.position = Vector2.Lerp(startPos, endPos, t);
				yield return null;
			}

			yield return new WaitForSeconds(upTime);

			t = 0;
			while (t < transitionTime)
			{
				t += Time.deltaTime / transitionTime;
				transform.position = Vector2.Lerp(endPos, startPos, t);
				yield return null;
			}

			yield return new WaitForSeconds(downTime);
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			collision.gameObject.GetComponent<KinematicPlayer>().PlayerDie();
		}
	}
}
