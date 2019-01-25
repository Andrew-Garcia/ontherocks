using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {

	
	public LayerMask groundLayerMask;

	public int[] playerLives;
	public GameObject[] playerPrefab;
	[SerializeField] string[] scenes;
	[SerializeField] AudioClip respawnClip;

	[SerializeField] float freezeTimeScale = 0.15f;
	[SerializeField] float freezeTimeDuration = 0.8f;

	bool gameStarted = false;
	public bool superPickUpActive = false;

	Vector2 findSpawnPoint() {
		List<Vector3> available = new List<Vector3>();
		foreach(GameObject rockObject in GameObject.FindGameObjectsWithTag("Rock"))
		{
			RockScript rock = rockObject.GetComponent<RockScript>();
			if (!rock)
                continue;
			Vector2 rockTop = rock.getTop();
			RaycastHit2D hit = Physics2D.Raycast(rockTop, Vector2.up, 2.5f, groundLayerMask);
	
			if (!hit.collider)
			{
				available.Add(rockTop);
			}
			else
			{
				if (hit.collider.gameObject.layer == 12) Debug.DrawRay(hit.point, Vector2.up, Color.red, 5f);
			}
		}
		if (available.Count == 0)
            return Vector3.zero;

		Vector2 pos = available[Random.Range(0, available.Count)];
		Debug.DrawRay(pos, Vector2.up, Color.cyan, 5f);
		return pos; // available[Random.Range(0, available.Count)];
	}

	// Use this for initialization
	void Start () {
		findSpawnPoint();
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
		if (!gameStarted && Input.anyKey) {
			gameStarted = true;
			GameObject.FindGameObjectWithTag("MainMenu").SetActive(false);

			GameObject lavaObject = GameObject.FindGameObjectWithTag("Lava");
			if (lavaObject)
			{
				Eruption lava = lavaObject.GetComponent<Eruption>();
				lava.StartCoroutine(lava.Erupt());
			}
		}
	}

	IEnumerator RespawnPlayer(int playerIndex) {
		yield return new WaitForSeconds(0.5f);

		Vector2 spawn = findSpawnPoint();
		AudioSource.PlayClipAtPoint(respawnClip, spawn, 1f);
		GameObject newPlayer = Instantiate(playerPrefab[playerIndex], spawn + Vector2.up * 2f, Quaternion.identity);
        //GameEye2D.Focus.F_Transform F_Transform = newPlayer.GetComponent<GameEye2D.Focus.F_Transform>();
		Camera.main.GetComponent<CameraFocus>().AddFocus(newPlayer.transform);
	}

	
	public IEnumerator FreezeFrame() {
        Time.timeScale = freezeTimeScale;
		//Time.fixedDeltaTime = freezeTimeScale * 0.02f;
        yield return new WaitForSecondsRealtime(freezeTimeDuration);
		//Time.fixedDeltaTime = 0.02f;
        Time.timeScale = 1;
	}

	public void freezeFrame() {
		StartCoroutine("FreezeFrame");
	}


	public void onPlayerDie(int playerIndex) {
		playerIndex = playerIndex - 1;
		int otherPlayerIndex = (playerIndex + 1) % 2;
		//Debug.Log(string.Format("Lives: player1: {0}\tplayer2: {1}", playerLives[0], playerLives[1]));
        --playerLives[playerIndex];

		if (playerLives[otherPlayerIndex] == 0) {
			//Time.fixedDeltaTime = 0.02f;
			Time.timeScale = 1;
            FindObjectOfType<LevelSelector>().LoadLevel();
		}

		if (playerLives[playerIndex] == 0) {
			GameObject.FindGameObjectWithTag("Winner").GetComponent<Image>().enabled = true;
		} else {
			StartCoroutine("RespawnPlayer", playerIndex);
		}


		foreach (PlayerLives script in GameObject.FindObjectsOfType<PlayerLives>()) {
			script.onPlayerLivesChange(playerLives);
		}

	}
}
