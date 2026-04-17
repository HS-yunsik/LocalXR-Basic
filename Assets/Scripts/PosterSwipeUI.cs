using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 전체화면 포스터 스와이프 갤러리 UI.
/// Viewport GameObject에 부착. 인스타그램식 스냅 페이징 구현.
/// </summary>
public class PosterSwipeUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [Tooltip("최상위 오버레이 패널 (show/hide 대상)")]
    [SerializeField] private GameObject overlay;

    [Tooltip("포스터들이 나열될 컨테이너 (Viewport의 자식)")]
    [SerializeField] private RectTransform posterContainer;

    [Tooltip("하단 페이지 점 인디케이터 부모 (Horizontal Layout Group 필요)")]
    [SerializeField] private Transform dotContainer;

    [Tooltip("닫기 버튼")]
    [SerializeField] private Button closeButton;

    [Header("Swipe Settings")]
    [Tooltip("스냅 애니메이션 속도 (클수록 빠름)")]
    [SerializeField] private float snapSpeed = 18f;

    [Tooltip("다음/이전 페이지로 넘어가는 최소 드래그 거리(px)")]
    [SerializeField] private float swipeThreshold = 60f;

    [Header("Dot Colors")]
    [SerializeField] private Color dotActiveColor   = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color dotInactiveColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private float dotActiveSize    = 12f;
    [SerializeField] private float dotInactiveSize  = 8f;

    // ── 내부 상태 ──────────────────────────────────────────────────────────
    private List<Image> posterImages    = new List<Image>();
    private List<Image> dotImages       = new List<Image>();
    private int         currentPage     = 0;
    private int         totalPages      = 0;
    private float       pageWidth       = 0f;
    private float       dragStartX      = 0f;
    private float       containerStartX = 0f;
    private Coroutine   snapCoroutine;

    private static readonly Color[] FallbackColors =
    {
        new Color(0.22f, 0.40f, 0.78f),
        new Color(0.78f, 0.28f, 0.22f),
        new Color(0.22f, 0.68f, 0.40f),
        new Color(0.78f, 0.52f, 0.18f),
        new Color(0.52f, 0.22f, 0.78f),
    };

    // ── Unity 생명주기 ─────────────────────────────────────────────────────
    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        // overlay는 씬에 비활성 상태로 저장되어 있으므로 여기서 건드리지 않음.
        // (SetActive(false) 호출 시 Viewport도 비활성화되어 코루틴 실행 불가)
    }

    // ── 공개 API ───────────────────────────────────────────────────────────

    /// <summary>ARPosterManager가 타겟 인식 시 호출.</summary>
    public void Show(PosterData data)
    {
        if (overlay == null || data == null) return;
        overlay.SetActive(true);
        // SetActive 직후 레이아웃을 동기식으로 강제 계산해 pageWidth를 올바르게 읽음
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        BuildPosters(data);
        GoToPage(0, false);
    }

    /// <summary>타겟 인식 해제 또는 닫기 버튼 시 호출.</summary>
    public void Hide()
    {
        if (overlay != null)
            overlay.SetActive(false);
        CleanUpPosters();
    }

    // ── 포스터 생성/제거 ───────────────────────────────────────────────────

    private void BuildPosters(PosterData data)
    {
        CleanUpPosters();

        pageWidth = GetComponent<RectTransform>().rect.width;

        int spriteCount = (data.posterSprites != null) ? data.posterSprites.Length : 0;
        totalPages = (spriteCount > 0) ? spriteCount : 3; // 최소 3장 플레이스홀더

        posterContainer.sizeDelta = new Vector2(pageWidth * totalPages,
                                                posterContainer.sizeDelta.y);

        for (int i = 0; i < totalPages; i++)
        {
            SpawnPosterItem(i, data, spriteCount);
            SpawnDotItem(i);
        }
        RefreshDots();
    }

    private void SpawnPosterItem(int index, PosterData data, int spriteCount)
    {
        GameObject item = new GameObject("Poster_" + index);
        item.transform.SetParent(posterContainer, false);

        RectTransform rt = item.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 1f);   // 세로 꽉 채움
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(pageWidth, 0f);
        rt.anchoredPosition = new Vector2(index * pageWidth + pageWidth * 0.5f, 0f);

        Image img         = item.AddComponent<Image>();
        img.raycastTarget = false; // 드래그 이벤트를 Viewport로 통과시킴

        bool hasSprite = spriteCount > 0
                      && index < spriteCount
                      && data.posterSprites[index] != null;

        if (hasSprite)
        {
            img.sprite = data.posterSprites[index];
            img.type   = Image.Type.Simple;
            AspectRatioFitter arf = item.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }
        else
        {
            img.color = PickPlaceholderColor(data, index);
            SpawnLabel(item, data.targetName, index);
        }
        posterImages.Add(img);
    }

    private void SpawnLabel(GameObject parent, string targetName, int index)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent.transform, false);

        RectTransform lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text          = "<b>" + targetName + "</b>\nPoster " + (index + 1);
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontSize      = 60;
        tmp.color         = new Color(1f, 1f, 1f, 0.85f);
        tmp.raycastTarget = false;
    }

    private void SpawnDotItem(int index)
    {
        GameObject dot = new GameObject("Dot_" + index);
        dot.transform.SetParent(dotContainer, false);

        RectTransform drt = dot.AddComponent<RectTransform>();
        drt.sizeDelta = Vector2.one * dotInactiveSize;

        Image img         = dot.AddComponent<Image>();
        img.color         = dotInactiveColor;
        img.raycastTarget = false;
        dotImages.Add(img);
    }

    private void CleanUpPosters()
    {
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        // 자식 GameObject 모두 제거
        int childCount = posterContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
            Destroy(posterContainer.GetChild(i).gameObject);

        int dotCount = dotContainer.childCount;
        for (int i = dotCount - 1; i >= 0; i--)
            Destroy(dotContainer.GetChild(i).gameObject);

        posterImages.Clear();
        dotImages.Clear();
        currentPage = 0;
        totalPages  = 0;
        pageWidth   = 0f;

        if (posterContainer != null)
            posterContainer.anchoredPosition = Vector2.zero;
    }

    // ── 페이지 이동 ────────────────────────────────────────────────────────

    private void GoToPage(int page, bool animate)
    {
        currentPage = Mathf.Clamp(page, 0, Mathf.Max(0, totalPages - 1));
        float targetX = -currentPage * pageWidth;

        if (snapCoroutine != null)
            StopCoroutine(snapCoroutine);

        if (animate)
            snapCoroutine = StartCoroutine(RunSnap(targetX));
        else
            posterContainer.anchoredPosition =
                new Vector2(targetX, posterContainer.anchoredPosition.y);

        RefreshDots();
    }

    /// <summary>지수 감속 보간 — 인스타그램 스냅 느낌.</summary>
    private IEnumerator RunSnap(float targetX)
    {
        while (Mathf.Abs(posterContainer.anchoredPosition.x - targetX) > 0.5f)
        {
            float nx = Mathf.Lerp(posterContainer.anchoredPosition.x,
                                  targetX,
                                  Time.unscaledDeltaTime * snapSpeed);
            posterContainer.anchoredPosition =
                new Vector2(nx, posterContainer.anchoredPosition.y);
            yield return null;
        }
        posterContainer.anchoredPosition =
            new Vector2(targetX, posterContainer.anchoredPosition.y);
        snapCoroutine = null;
    }

    private void RefreshDots()
    {
        for (int i = 0; i < dotImages.Count; i++)
        {
            bool active = (i == currentPage);
            dotImages[i].color = active ? dotActiveColor : dotInactiveColor;
            dotImages[i].GetComponent<RectTransform>().sizeDelta =
                Vector2.one * (active ? dotActiveSize : dotInactiveSize);
        }
    }

    // ── 드래그 이벤트 ──────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }
        dragStartX      = eventData.position.x;
        containerStartX = posterContainer.anchoredPosition.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = eventData.position.x - dragStartX;
        posterContainer.anchoredPosition =
            new Vector2(containerStartX + delta, posterContainer.anchoredPosition.y);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float delta     = eventData.position.x - dragStartX;
        int   nextPage  = currentPage;
        if (delta < -swipeThreshold)     nextPage = currentPage + 1;
        else if (delta > swipeThreshold) nextPage = currentPage - 1;
        GoToPage(nextPage, true);
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────

    private Color PickPlaceholderColor(PosterData data, int index)
    {
        if (data.placeholderColors != null && index < data.placeholderColors.Length)
            return data.placeholderColors[index];
        return FallbackColors[index % FallbackColors.Length];
    }
}
