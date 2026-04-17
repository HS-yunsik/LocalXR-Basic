using UnityEngine;

/// <summary>
/// 각 이미지 타겟에 연결할 포스터 데이터 ScriptableObject.
/// Project 창에서 우클릭 → AR Posters → Poster Data 로 생성.
/// </summary>
[CreateAssetMenu(fileName = "PosterData", menuName = "AR Posters/Poster Data")]
public class PosterData : ScriptableObject
{
    [Tooltip("Inspector 식별용 타겟 이름 (예: Target_01)")]
    public string targetName = "Target";

    [Tooltip("표시할 포스터 이미지 목록 (비워두면 컬러 플레이스홀더 사용)")]
    public Sprite[] posterSprites;

    [Tooltip("포스터가 없을 때 사용할 플레이스홀더 색상 (순서대로 적용)")]
    public Color[] placeholderColors = new Color[]
    {
        new Color(0.22f, 0.40f, 0.78f, 1f),
        new Color(0.78f, 0.28f, 0.22f, 1f),
        new Color(0.22f, 0.68f, 0.40f, 1f),
        new Color(0.78f, 0.52f, 0.18f, 1f),
        new Color(0.52f, 0.22f, 0.78f, 1f),
    };
}
