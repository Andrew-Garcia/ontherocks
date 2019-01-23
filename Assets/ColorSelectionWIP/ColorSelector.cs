using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorSelector : MonoBehaviour {

    [SerializeField] Texture2D colorMap;
    [SerializeField] Image playerImage;

    [SerializeField] float rotateSpeed = 1;

	void Update () {
        RotationInput();
        ColorConverter();
    }

    private void ColorConverter()
    {
        Color currentColor = colorMap.GetPixel(Mathf.FloorToInt(transform.localEulerAngles.z), 1);
        playerImage.color = currentColor;
    }

    private void RotationInput()
    {
        transform.Rotate(0f, 0f, -Input.GetAxis("Player1Horizontal") * rotateSpeed);
        Debug.Log("transform.localeulerangles.z: " + Mathf.FloorToInt(transform.localEulerAngles.z));
    }
}
