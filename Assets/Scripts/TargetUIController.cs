using UnityEngine;
using Vuforia;

/// <summary>
/// 각 ImageTarget GameObject에 부착.
/// 타겟 인식 시 ARPosterManager에 포스터 표시 요청,
/// 인식 해제 시 숨기기 요청.
/// </summary>
public class TargetUIController : MonoBehaviour
{
    [Tooltip("이 타겟에 연결된 포스터 데이터 (Project 창에서 생성한 PosterData 에셋)")]
    [SerializeField] private PosterData posterData;

    private ObserverBehaviour observer;

    private void Start()
    {
        observer = GetComponent<ObserverBehaviour>();

        if (observer != null)
            observer.OnTargetStatusChanged += OnStatusChanged;
        else
            Debug.LogWarning("[TargetUIController] ObserverBehaviour not found on " + gameObject.name);

        if (posterData == null)
            Debug.LogWarning("[TargetUIController] PosterData not assigned on " + gameObject.name);
    }

    private void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked = status.Status == Status.TRACKED
                      || status.Status == Status.EXTENDED_TRACKED
                      || status.Status == Status.LIMITED;

        if (ARPosterManager.Instance == null) return;

        if (isTracked)
        {
            ARPosterManager.Instance.ShowPosters(posterData);
        }
        else
        {
            string name = (posterData != null) ? posterData.targetName : "";
            ARPosterManager.Instance.HidePosters(name);
        }
    }

    private void OnDestroy()
    {
        if (observer != null)
            observer.OnTargetStatusChanged -= OnStatusChanged;
    }
}
