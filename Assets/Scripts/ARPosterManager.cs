using UnityEngine;

/// <summary>
/// 전체화면 포스터 UI를 중앙에서 제어하는 싱글톤.
/// PosterOverlay Canvas와 함께 씬에 하나만 배치.
/// </summary>
public class ARPosterManager : MonoBehaviour
{
    public static ARPosterManager Instance { get; private set; }

    [SerializeField] private PosterSwipeUI posterSwipeUI;

    // 현재 포스터를 띄운 타겟 이름 추적 (멀티 타겟 충돌 방지)
    private string currentTargetName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 이미지 타겟 인식 시 호출. 해당 타겟의 포스터를 전체화면으로 표시.
    /// </summary>
    public void ShowPosters(PosterData data)
    {
        if (data == null) return;
        currentTargetName = data.targetName;
        posterSwipeUI.Show(data);
    }

    /// <summary>
    /// 이미지 타겟 인식 해제 시 호출. 해당 타겟이 현재 표시 중일 때만 닫음.
    /// </summary>
    public void HidePosters(string targetName)
    {
        if (currentTargetName != targetName) return;
        currentTargetName = null;
        posterSwipeUI.Hide();
    }
}
