using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLives : MonoBehaviour {

	[SerializeField] Sprite[] lifeSprites;

	[SerializeField] int playerNumber;
	int currentCount;
	Image image;

	private void Awake() {
		image = GetComponent<Image>();
	}
	public void onPlayerLivesChange(int[] newLives) {
		if (currentCount == newLives[playerNumber - 1])
			return;
		currentCount = newLives[playerNumber - 1];
		if (currentCount > 0 && currentCount < lifeSprites.Length)
            image.sprite = lifeSprites[currentCount - 1];
		else if (currentCount == 0)
			image.enabled = false;
	}
}
