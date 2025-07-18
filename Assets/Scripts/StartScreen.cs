using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    public List<Sprite> sprites;
    public SpriteRenderer sr;
    public float currentIndex;
    public int currentAnim = -1;
    public float animSpeed = 0.25f;
    List<List<int>> anims = new List<List<int>>()
    {
        new List<int>(){2,3,4},
        new List<int>(){11,12,13,14},
        new List<int>(){6,7,8,9,10,11,12,13,14},

    };
    // Start is called before the first frame update
    /*
     * 
look up 2,3,4,1
switch tile 10,11,12,13,14,1
place tile 6,7,8,9,10,11,12,13,14,1
    1 - 4 look up at sky 
5 - 10 place tile
10 - 14 switch tile

Frame 1 or 5 can jump straight to 11 to switch tile instead of looking/placing
1 and 5 are essentially the same frame, 14 can go to either 1 or 5
     */
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int index = 1;
        currentIndex += animSpeed * (Time.deltaTime * (1f / 60f));
       
        if (currentAnim == -1)
        {
            if(currentIndex > 1f && currentIndex > (Random.value * 4f))
            {
                currentAnim = Random.Range(0, anims.Count);
                currentIndex = 0;
            }
        }
        else
        {
            if (Mathf.FloorToInt(currentIndex) > anims[currentAnim].Count-1)
            {
                currentAnim = -1;
                currentIndex = 0;
            }
            else
            {
                index = anims[currentAnim][Mathf.FloorToInt(currentIndex)];
            }
            
        }
        sr.sprite = sprites[index-1];
        if(Services.GameController.gameState == GameState.Start)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Services.GameController.GameStateGameplay();
            }
        }

    }
}
