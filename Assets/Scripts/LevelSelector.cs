using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour  {

    [SerializeField] int menuSceneIndex = 0;
    [SerializeField] LevelsSO[] levels;

    [SerializeField] int worlds = 3;
    [SerializeField] int levelsPerWorld = 3;
    [Header("Attached Objects")]
    [SerializeField] GameObject menu;
    [SerializeField] Image levelImage;
    [SerializeField] Image playImage;
    [Header("Play Type Sprites")]
    [SerializeField] Sprite playSprite;
    [SerializeField] Sprite shuffleSprite;
    [SerializeField] Sprite loopSprite;
    [SerializeField] Sprite loopOneSprite;


    LevelsSO currentLevel;
    bool inMenu;
    int currentWorldNumber = 1;
    int currentLevelNumber = 1;
    int levelNumber;
    int worldNumber;
    int levelIndex = 0;
    int playTypeIndex = 1;
    bool usingHAxis;
    bool usingVAxis;



    //There can be only one! Keeps the old game object, destroys the new one - necessary for starting the game not from the menu
    private void Awake()
    {
        int selectorCount = FindObjectsOfType<LevelSelector>().Length;
        if (selectorCount > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        if (menuSceneIndex == SceneManager.GetActiveScene().buildIndex)
        {
            inMenu = true;
        }
        else
        {
            inMenu = false;
            menu.SetActive(false);
        }
        currentLevel = levels[levelIndex];
        levelImage.sprite = currentLevel.GetScreencap();
    }

    void Update () {
        if (inMenu)
        {
            MenuControls();
        }  
    }

    void MenuControls()
    {
        if (Input.GetAxisRaw("Player1Vertical") == 1 && !usingVAxis)
        {
            usingVAxis = true;
            if (currentWorldNumber != worlds)
            {
                currentWorldNumber++;
            }
            else
            {
                currentWorldNumber = 1;
            }
            GetCurrentLevel();
        }
        if (Input.GetAxisRaw("Player1Vertical") == -1 && !usingVAxis)
        {
            usingVAxis = true;
            if (currentWorldNumber != 1)
            {
                currentWorldNumber--;
            }
            else
            {
                currentWorldNumber = worlds;
            }
            GetCurrentLevel();
        }
        if (Input.GetAxisRaw("Player1Vertical") == 0)
        {
            usingVAxis = false;
        }

        if (Input.GetAxisRaw("Player1Horizontal") == 1 && !usingHAxis)
        {
            usingHAxis = true;
            if (currentLevelNumber != levelsPerWorld)
            {
                currentLevelNumber++;
            }
            else
            {
                currentLevelNumber = 1;
            }
            GetCurrentLevel();
        }

        if (Input.GetAxisRaw("Player1Horizontal") == -1 && !usingHAxis)
        {
            usingHAxis = true;
            if (currentLevelNumber != 1)
            {
                currentLevelNumber--;
            }
            else
            {
                currentLevelNumber = levelsPerWorld;
            }
            GetCurrentLevel();
        }
        if (Input.GetAxisRaw("Player1Horizontal") == 0)
        {
            usingHAxis = false;
        }

        if (Input.GetButtonDown("Player1Punch"))
        {
            if (playTypeIndex != 4)
            {
                playTypeIndex ++;
            }
            else
            {
                playTypeIndex = 1;
            }
            DisplayPlayType();
        }

        if (Input.GetButtonDown("Player1Jump"))
        {
            LoadLevel();
        }
    }
    //Scans for a level that matches the index
    void GetCurrentLevel()
    {
        while (levelNumber != currentLevelNumber || worldNumber != currentWorldNumber )
        {
            if (levelIndex <= levels.Length-2)
            {
                levelIndex++;
            }
            else
            {
                levelIndex = 0;
            }
            levelNumber = levels[levelIndex].GetLevelNumber();
            worldNumber = levels[levelIndex].GetWorldNumber();
        }
        currentLevel = levels[levelIndex];
        levelImage.sprite = currentLevel.GetScreencap();
    }
    //Displays the sprite for the symbol associated with each play type
    void DisplayPlayType()
    {
        if (playTypeIndex == 1) { playImage.sprite = playSprite; }
        if (playTypeIndex == 2) { playImage.sprite = shuffleSprite; }
        if (playTypeIndex == 3) { playImage.sprite = loopSprite; }
        if (playTypeIndex == 4) { playImage.sprite = loopOneSprite; }
    }

    public void LoadLevel()
    {
        //Hitting normal play in menu
        if (playTypeIndex == 1 && SceneManager.GetActiveScene().buildIndex == menuSceneIndex)
        {
            int currentLevelIndex = currentLevel.GetSceneIndex();
            SceneManager.LoadScene(currentLevelIndex);
            inMenu = false;
            menu.SetActive(false);
        }
        //Return to menu while in normal play mode
        else if (playTypeIndex == 1 && SceneManager.GetActiveScene().buildIndex != menuSceneIndex)
        {
            SceneManager.LoadScene(menuSceneIndex);
            inMenu = true;
            menu.SetActive(true);
        }
        //Hitting shuffle in menu
        else if (playTypeIndex == 2)
        {
            int randomLevelIndex = Random.Range(0, levels.Length);
            var randomLevel = levels[randomLevelIndex];
            int currentLevelIndex = randomLevel.GetSceneIndex();
            currentLevelNumber = randomLevel.GetLevelNumber();
            currentWorldNumber = randomLevel.GetWorldNumber();
            SceneManager.LoadScene(currentLevelIndex);
            inMenu = false;
            menu.SetActive(false);
        }
        //While on loop (world)
        else if (playTypeIndex == 3)
        {
            //While on loop and not in menu
            if (SceneManager.GetActiveScene().buildIndex != menuSceneIndex)
            {
                if (currentLevelNumber != levelsPerWorld)
                {
                    currentLevelNumber++;
                }
                else
                {
                    currentLevelNumber = 1;
                }

                GetCurrentLevel();
            }

            int currentLevelIndex = currentLevel.GetSceneIndex();
            SceneManager.LoadScene(currentLevelIndex);
            inMenu = false;
            menu.SetActive(false);
        }
        //While on loop1
        else if (playTypeIndex == 4)
        {
            int currentLevelIndex = currentLevel.GetSceneIndex();
            SceneManager.LoadScene(currentLevelIndex);
            inMenu = false;
            menu.SetActive(false);
        }
    }

    //Called when player pauses
    public void LoadMenu()
    {
        SceneManager.LoadScene(menuSceneIndex);
        inMenu = true;
        menu.SetActive(true);
    }
}
