using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinnerScript : MonoBehaviour {

	[SerializeField] float updateTime;
	// Use this for initialization
	void Start () {
		StartCoroutine("MoveText");
	}

	IEnumerator MoveText() {
		RectTransform rectTransform = GetComponent<RectTransform>();
		Vector2 size = rectTransform.rect.size;
		while (true) {
			yield return new WaitForSeconds(updateTime);
			transform.position = new Vector2(
				Random.Range(0, Screen.width - size.x),
				Random.Range(0, Screen.height - size.y)
			);
		}
	}
}
