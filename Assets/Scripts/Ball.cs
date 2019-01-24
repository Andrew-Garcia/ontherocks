using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour {

    [Header ("Bounding Box")]
    [SerializeField] float xMin, xMax, yMin, yMax;

    [SerializeField] float rotSpeed = 50;
    [SerializeField] float maxMoveSpeed = 10;
    [SerializeField] float minMoveSpeed = 1;
    [SerializeField] float closeEnough = 2;

    [SerializeField] Rigidbody2D rigBod;

	CircleCollider2D circleCol;
	SpriteRenderer sr;

	Transform childSprite;
	Quaternion childRot;

    Vector3 targetLoc;
    
    void Start () {
		targetLoc = new Vector3(Random.Range(xMin, xMax), Random.Range(yMin, yMax), 0f);

		circleCol = GetComponent<CircleCollider2D>();
		sr = GetComponentInChildren<SpriteRenderer>();

		childSprite = transform.GetChild(0);
		childRot = childSprite.rotation;

		StartCoroutine(OnSpawnBall());
	}
	
	void Update () {
        //slow down when close to target
        float moveSpeed = (Mathf.Clamp(Vector3.Distance(transform.position, targetLoc), minMoveSpeed, maxMoveSpeed));

        //look at target
        float step = rotSpeed * Time.deltaTime;
        Vector3 dir = targetLoc - transform.position;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
        Quaternion q = Quaternion.AngleAxis(targetAngle, Vector3.forward);
        transform.rotation = Quaternion.Lerp(transform.rotation, q, step * Time.deltaTime);

        //move toward target
        rigBod.velocity = transform.up * moveSpeed;

        //check if close enough to location to find new location
        if (Vector3.Distance(transform.position, targetLoc) < closeEnough)
        {
            targetLoc = new Vector3(Random.Range(xMin, xMax), Random.Range(yMin, yMax), 0f);
        }

		childSprite.rotation = childRot;
    }

	IEnumerator OnSpawnBall()
	{
		float i = 0;
		while (i < 1)
		{
			i += Time.deltaTime / 1.5f;

			if (i % 0.1f < 0.05f) sr.enabled = true;
			else sr.enabled = false;

			yield return null;
		}

		circleCol.enabled = true;
	}
}
