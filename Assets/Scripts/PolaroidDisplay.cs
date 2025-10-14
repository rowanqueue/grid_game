using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Logic;
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

    public IEnumerator ShowSnapshotRoutine(Dictionary<Vector2Int, Logic.Tile> tiles)
    {
        IsAnimating = true;
        SetTiles(tiles);
        polaroidParent.transform.localPosition = new Vector3(polaroidParent.transform.localPosition.x, defaultYpos, polaroidParent.transform.localPosition.z);
        dimPanel.color = new Color(dimPanel.color.r, dimPanel.color.g, dimPanel.color.b, 0);
        dimPanel.gameObject.SetActive(true);
        yield return polaroidFlash.FlashRoutine();
        dimPanel.DOFade(dimPanelAlpha, dimPanelFadeInTime).SetEase(Ease.Linear).Play();
        yield return ShowPolaroid();
    }

    public IEnumerator HideSnapshotRoutine()
    {
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

        yield return polaroidParent.transform.DOMoveY(endYpos / 2, animateDownTime / 2).SetEase(Ease.Linear).WaitForCompletion();
        polaroidParent.transform.DORotate(new Vector3(0, 0, zRotation), animateDownTime / 2).SetEase(Ease.OutQuad).Play();
        yield return polaroidParent.transform.DOMoveY(endYpos, animateDownTime / 2).SetEase(Ease.OutQuad).WaitForCompletion();
    }

    private IEnumerator HidePolaroid()
    {
        dimPanel.DOFade(0, dimPanelFadeOutTime).SetEase(Ease.Linear).Play();
        yield return polaroidParent.transform.DOMoveY(defaultYpos, animateUpTime).SetEase(Ease.InQuad).WaitForCompletion();
        polaroidParent.SetActive(false);
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
