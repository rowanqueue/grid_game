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

    public float ActionClipSpeed = 1.5f;
    public float LookUpClipSpeed = 1f;
    public float IdleHoldMultiplier = 1.5f;
    public float LookUpHoldMultiplier = 1.25f;

    [Header("Random Idle")]
    public float minIdleDelay = 0.5f;
    public float maxIdleDelay = 2f;

    [Header("Cloud Parallax")]
    public SpriteRenderer frontClouds;
    public SpriteRenderer backClouds;
    public float frontCloudSpeed = 0.15f;
    public float backCloudSpeed = 0.05f;

    List<List<int>> anims = new List<List<int>>()
    {
        // 0: holding tile, look up/down
        new List<int>(){1, 2, 3, 2, 1},
        // 1: holding tile, place it (ends on empty-hands idle)
        new List<int>(){1, 6, 7, 8,8, 9},
        // 2: not holding tile, look up/down
        new List<int>(){9, 10, 9},
        // 3: not holding tile, pick up (ends on holding idle)
        new List<int>(){9,10, 11, 12, 13, 14, 1},
    };
    List<int> frameDurations = new List<int>
    {
        1,1,1,1,1,1,1,1,1,1,1,1,1,1
    };

    bool holdingTile = true;
    float idleTimer;
    bool idling;

    Transform frontCloudA;
    Transform frontCloudB;
    Transform backCloudA;
    Transform backCloudB;
    float frontCloudWidth;
    float backCloudWidth;
    float frontScroll;
    float backScroll;
    Vector3 frontOrigin;
    Vector3 backOrigin;
    Transform cloudParent;

    /*
     * Random state-aware clips (with idle pauses between them):
     *
     * Holding: 0 look (1,2,3,2,1) or 1 place (1,6,7,8,9)
     * Empty:   2 look (9,10,9) or 3 pick up (9,10,11,12,13,14,1)
     */
    void Start()
    {
        holdingTile = true;
        currentAnim = -1;
        currentIndex = 0;
        BeginIdle();
        SetupClouds();
    }

    bool IsRestingFrame(int frame) => frame == 1 || frame == 9;

    bool IsLookUpPeakFrame(int frame) => frame == 3 || frame == 10;

    bool IsActionClip(int anim) => anim == 1 || anim == 3;

    bool IsLookUpClip(int anim) => anim == 0 || anim == 2;

    void BeginIdle()
    {
        idling = true;
        idleTimer = Random.Range(minIdleDelay, maxIdleDelay);
        currentAnim = -1;
        currentIndex = 0;
    }

    void PickNextAnim()
    {
        if (holdingTile)
            currentAnim = Random.value < 0.5f ? 0 : 1;
        else
            currentAnim = Random.value < 0.5f ? 2 : 3;
        currentIndex = 0;
        idling = false;
    }

    void FinishClip()
    {
        if (currentAnim == 1)
            holdingTile = false;
        else if (currentAnim == 3)
            holdingTile = true;
        BeginIdle();
    }

    int IdleFrame() => holdingTile ? 1 : 9;

    void SetupClouds()
    {
        if (frontClouds == null || backClouds == null)
            return;

        cloudParent = frontClouds.transform.parent;

        // Detach back from front so layers move independently.
        Vector3 backWorld = backClouds.transform.position;
        backClouds.transform.SetParent(cloudParent, true);
        backClouds.transform.position = backWorld;

        frontCloudA = frontClouds.transform;
        backCloudA = backClouds.transform;
        frontOrigin = frontCloudA.position;
        backOrigin = backCloudA.position;
        frontScroll = 0f;
        backScroll = 0f;

        frontCloudWidth = frontClouds.bounds.size.x;
        backCloudWidth = backClouds.bounds.size.x;

        frontCloudB = DuplicateCloud(frontClouds, frontCloudWidth);
        backCloudB = DuplicateCloud(backClouds, backCloudWidth);
    }

    Transform DuplicateCloud(SpriteRenderer source, float width)
    {
        GameObject copy = Instantiate(source.gameObject, source.transform.parent);
        copy.name = source.gameObject.name + "_Loop";
        copy.transform.position = source.transform.position + Vector3.right * width;

        SpriteRenderer copySr = copy.GetComponent<SpriteRenderer>();
        if (copySr != null)
        {
            copySr.sprite = source.sprite;
            copySr.sharedMaterial = source.sharedMaterial;
            copySr.sortingLayerID = source.sortingLayerID;
            copySr.sortingOrder = source.sortingOrder;
            copySr.color = source.color;
        }

        // Strip nested children so a front duplicate doesn't clone the old back child.
        for (int i = copy.transform.childCount - 1; i >= 0; i--)
            Destroy(copy.transform.GetChild(i).gameObject);

        return copy.transform;
    }

    void UpdateClouds()
    {
        if (frontCloudA == null || backCloudA == null)
            return;

        MoveCloudPair(frontCloudA, frontCloudB, ref frontScroll, frontOrigin, frontCloudSpeed, frontCloudWidth);
        MoveCloudPair(backCloudA, backCloudB, ref backScroll, backOrigin, backCloudSpeed, backCloudWidth);
    }

    void MoveCloudPair(Transform a, Transform b, ref float scroll, Vector3 origin, float speed, float width)
    {
        if (a == null || b == null || width <= 0f)
            return;

        scroll += speed * Time.deltaTime;
        float wrapped = Mathf.Repeat(scroll, width);
        a.position = new Vector3(origin.x - wrapped, origin.y, origin.z);
        b.position = new Vector3(origin.x - wrapped + width, origin.y, origin.z);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateClouds();

        int index = IdleFrame();

        if (idling)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
                PickNextAnim();
        }

        if (!idling && currentAnim >= 0)
        {
            List<int> clip = anims[currentAnim];
            int frameSlot = Mathf.FloorToInt(currentIndex);
            if (frameSlot > clip.Count - 1)
                frameSlot = clip.Count - 1;

            index = clip[frameSlot];
            float animTotal = frameDurations[index - 1];
            if (IsRestingFrame(index))
                animTotal *= IdleHoldMultiplier;
            else if (IsLookUpPeakFrame(index))
                animTotal *= LookUpHoldMultiplier;

            float clipSpeed = 1f;
            if (IsActionClip(currentAnim) && !IsRestingFrame(index))
                clipSpeed = ActionClipSpeed;
            else if (IsLookUpClip(currentAnim) && !IsRestingFrame(index))
                clipSpeed = LookUpClipSpeed;

            currentIndex += (animSpeed * clipSpeed * (Time.deltaTime * (1f / 60f))) / animTotal;

            if (Mathf.FloorToInt(currentIndex) > clip.Count - 1)
            {
                FinishClip();
                index = IdleFrame();
            }
            else
            {
                index = clip[Mathf.FloorToInt(currentIndex)];
            }
        }

        sr.sprite = sprites[index - 1];
        if (Services.GameController.gameState == GameState.Start)
        {
            if (InputHelper.GetPrimaryPressBegan())
            {
                Services.AudioManager.StartMusic();
                Services.GameController.GameStateGameplay();
            }
        }
    }
}
