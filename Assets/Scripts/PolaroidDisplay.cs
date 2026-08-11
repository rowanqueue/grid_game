using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Logic;
using Save;
using UnityEngine;

public class PolaroidDisplay : MonoBehaviour
{
    public bool IsAnimating { get; private set; } = false;

    [Header("References")]
    [SerializeField] private PolaroidFlash polaroidFlash;
    [SerializeField] private GameObject polaroidParent;
    [SerializeField] private List<PolaroidToken> polaroidToken;
    [SerializeField] private SpriteRenderer dimPanel;

    [Header("Animation Parameters")]
    [SerializeField] private float defaultYpos;
    [SerializeField] private float endYpos;
    [SerializeField] private float animateDownTime = 0.5f;
    [SerializeField] private float animateUpTime = 0.25f;
    [SerializeField] private float waitTime = 0.5f;
    [SerializeField] private float polaroidRotationRange = 5f;
    [SerializeField] private float dimPanelFadeInTime = 0.1f;
    [SerializeField] private float dimPanelFadeOutTime = 0.5f;
    [SerializeField] private float dimPanelAlpha = 0.5f;

    [Header("Load Snapshot Preview")]
    [SerializeField] private bool staticPreviewMode;

    void Awake()
    {
        if (!staticPreviewMode)
        {
            return;
        }
        if (polaroidFlash != null)
        {
            polaroidFlash.gameObject.SetActive(false);
        }
        if (dimPanel != null)
        {
            dimPanel.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (staticPreviewMode)
        {
            RefreshStaticPreview();
        }
    }

    public void RefreshStaticPreview()
    {
        if (Services.GameController == null || Services.Visuals == null)
        {
            return;
        }
        if (!SaveLoad.HasSave(1))
        {
            return;
        }
        Logic.History.Turn turn = SaveLoad.PeekTurn(1);
        if (turn == null)
        {
            return;
        }
        ShowStaticFromTurn(turn);
    }

    public void ShowStaticFromTurn(Logic.History.Turn turn)
    {
        if (turn == null || turn.grid == null)
        {
            return;
        }
        KillSnapshotTweens();
        if (polaroidFlash != null)
        {
            polaroidFlash.gameObject.SetActive(false);
        }
        if (dimPanel != null)
        {
            dimPanel.gameObject.SetActive(false);
        }
        SetTilesFromGrid(turn.grid);
        if (polaroidParent != null)
        {
            polaroidParent.SetActive(true);
            Vector3 pos = polaroidParent.transform.localPosition;
            polaroidParent.transform.localPosition = new Vector3(pos.x, endYpos, pos.z);
            polaroidParent.transform.localRotation = Quaternion.identity;
        }
    }

    public void SetTilesFromGrid(List<TokenData> grid)
    {
        foreach (PolaroidToken token in polaroidToken)
        {
            token.ResetDisplay();
        }
        if (grid == null)
        {
            return;
        }
        for (int i = 0; i < grid.Count; i++)
        {
            if (grid[i] == Logic.History.nullToken)
            {
                continue;
            }
            int x = i % 5;
            int y = i / 5;
            PolaroidToken token = polaroidToken[PositionToIndex(new Vector2Int(x, y))];
            token.gameObject.SetActive(true);
            token.InitializeDisplay(grid[i]);
        }
    }

    public IEnumerator ShowSnapshotRoutine(Dictionary<Vector2Int, Logic.Tile> tiles)
    {
        IsAnimating = true;
        KillSnapshotTweens();
        SetTiles(tiles);
        polaroidParent.transform.localPosition = new Vector3(polaroidParent.transform.localPosition.x, defaultYpos, polaroidParent.transform.localPosition.z);
        dimPanel.color = new Color(dimPanel.color.r, dimPanel.color.g, dimPanel.color.b, 0);
        dimPanel.gameObject.SetActive(true);
        yield return polaroidFlash.FlashRoutine();
        dimPanel.DOFade(dimPanelAlpha, dimPanelFadeInTime).SetEase(Ease.Linear).Play();
        yield return ShowPolaroid();
        IsAnimating = false;
    }

    public IEnumerator HideSnapshotRoutine()
    {
        IsAnimating = true;
        KillSnapshotTweens();
        yield return HidePolaroid();
        dimPanel.gameObject.SetActive(false);
        foreach (PolaroidToken token in polaroidToken)
        {
            token.ResetDisplay();
        }
        IsAnimating = false;
    }

    private IEnumerator ShowPolaroid()
    {
        polaroidParent.SetActive(true);
        polaroidParent.transform.localPosition = new Vector3(polaroidParent.transform.localPosition.x, defaultYpos, polaroidParent.transform.localPosition.z);
        float zRotation = Random.Range(-polaroidRotationRange, polaroidRotationRange);

        yield return polaroidParent.transform.DOLocalMoveY(endYpos / 2, animateDownTime / 2).SetEase(Ease.Linear).WaitForCompletion();
        polaroidParent.transform.DORotate(new Vector3(0, 0, zRotation), animateDownTime / 2).SetEase(Ease.OutQuad).Play();
        yield return polaroidParent.transform.DOLocalMoveY(endYpos, animateDownTime / 2).SetEase(Ease.OutQuad).WaitForCompletion();
    }

    private IEnumerator HidePolaroid()
    {
        dimPanel.DOFade(0, dimPanelFadeOutTime).SetEase(Ease.Linear).Play();
        yield return polaroidParent.transform.DOLocalMoveY(defaultYpos, animateUpTime).SetEase(Ease.InQuad).WaitForCompletion();
        polaroidParent.SetActive(false);
    }

    private void KillSnapshotTweens()
    {
        if (polaroidParent != null)
        {
            polaroidParent.transform.DOKill();
        }
        if (dimPanel != null)
        {
            dimPanel.DOKill();
        }
    }

    public void SetTiles(Dictionary<Vector2Int, Logic.Tile> tiles)
    {

        foreach (PolaroidToken token in polaroidToken)
        {
            token.ResetDisplay();
        }

        foreach (Vector2Int pos in tiles.Keys)
        {
            // If there is no token, skip
            if (tiles[pos].token == null) continue;

            TokenData tokenData = tiles[pos].token.data;
            PolaroidToken token = polaroidToken[PositionToIndex(pos)];
            token.gameObject.SetActive(true);
            token.InitializeDisplay(tokenData);

        }
    }

    private int PositionToIndex(Vector2Int pos)
    {
        return pos.x + ((4 - pos.y) * 5);
    }
}
