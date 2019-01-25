using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Level ScriptableObject")]
public class LevelsSO : ScriptableObject {

    [SerializeField] int worldNumber;
    [SerializeField] int levelNumber;
    [SerializeField] int sceneIndex;
    [SerializeField] Sprite screencap;

    public int GetWorldNumber() { return worldNumber; }
    public int GetLevelNumber() { return levelNumber; }
    public int GetSceneIndex() { return sceneIndex; }
    public Sprite GetScreencap() { return screencap; }
}
