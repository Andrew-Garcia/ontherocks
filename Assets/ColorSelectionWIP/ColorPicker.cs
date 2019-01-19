using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour {

    [SerializeField] Texture2D colorMap;
    [SerializeField] SpriteRenderer player;
    [SerializeField] float speed = 1f;

    [SerializeField] float colorMapX;
    [SerializeField] float colorMapY;

    Vector3 crosshairPosition;

    // Update is called once per frame
    void Update() {
        CrosshairMove();       
        Color currentColor = colorMap.GetPixel(Mathf.FloorToInt(transform.position.x +200), Mathf.FloorToInt(transform.position.y +200));
        player.color = currentColor;
        Debug.Log(currentColor);
    }

    void CrosshairMove(){       
        crosshairPosition.x += (Mathf.Clamp((Input.GetAxis("Player1Horizontal") * speed * Time.deltaTime), -200f, 200f));
        crosshairPosition.y += (Mathf.Clamp((Input.GetAxis("Player1Vertical") * speed * Time.deltaTime), -200f, 200f));
        transform.position = crosshairPosition;
    }
}
