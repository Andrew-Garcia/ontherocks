using UnityEngine;
using System.Collections;
using UnityEngine.Video;

public class quadPlay : MonoBehaviour
{

#if !(UNITY_PS4 || UNITY_IOS || UNITY_XBOXONE || UNITY_ANDROID)
	public VideoPlayer movTexture;
#endif

	// Use this for initialization
	void Start()
	{

#if !(UNITY_PS4 || UNITY_IOS || UNITY_XBOXONE || UNITY_ANDROID)

		VideoPlayer t = new VideoPlayer();
		t.targetTexture = (RenderTexture)this.GetComponent<Renderer>().material.mainTexture;
		t.isLooping = true;
		t.Play();

#endif
	}

	// Update is called once per frame
	void Update()
	{

	}

}
