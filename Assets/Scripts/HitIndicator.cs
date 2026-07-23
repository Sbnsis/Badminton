using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 屏幕正中心的瞄准圈 UI
/// - 大小：球离玩家越远圈越大，越近越小
/// - 颜色：球不可击中时灰色；可击中时，离最佳距离越近越绿越远越红；
///         刚好在最佳距离范围内时变橙色
/// </summary>
public class HitIndicator : MonoBehaviour
{
    [Header("引用")]
    public Transform playerTransform;       // 玩家位置
    public Shuttlecock shuttlecock;         // 羽毛球
    public RectTransform circleRect;        // 圈的 RectTransform
    public Image circleImage;               // 圈的 Image 组件

    [Header("距离参数")]
    public float maxHitDistance = 3f;       // 球可被击中的最远距离
    public float optimalHitDistance = 1.5f; // 最佳击球距离
    public float optimalRange = 0.4f;       // 最佳距离 ± 范围（橙色区间）

    [Header("圈大小")]
    public float maxCircleSize = 200f;      // 圈最大尺寸（球最远时）
    public float minCircleSize = 30f;       // 圈最小尺寸（球最近时）
    public float maxConsiderDistance = 10f; // 用于计算圈大小的最大参考距离
    public float sizeSmoothSpeed = 10f;     // 圈大小变化平滑度

    [Header("颜色")]
    public Color cantHitColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);  // 无法击中：半透明灰
    public Color redColor = new Color(1f, 0.2f, 0.2f, 0.85f);        // 远（差）→ 红
    public Color greenColor = new Color(0.2f, 1f, 0.3f, 0.85f);      // 近（好）→ 绿
    public Color orangeColor = new Color(1f, 0.55f, 0f, 0.9f);       // 最佳范围 → 橙
    public float colorSmoothSpeed = 8f;

    [Header("圆环外观")]
    public float ringThickness = 0.15f;      // 圆环粗细（0~1，占半径的比例）
    public int ringResolution = 128;         // 纹理分辨率

    // 内部状态
    private float currentSize;
    private Color currentColor;

    void Start()
    {
        if (playerTransform == null)
            playerTransform = transform;

        currentSize = maxCircleSize;
        currentColor = cantHitColor;

        // 确保圈在屏幕正中心
        if (circleRect != null)
            circleRect.anchoredPosition = Vector2.zero;

        // 如果没有 sprite，自动生成圆环贴图
        if (circleImage != null && circleImage.sprite == null)
        {
            circleImage.sprite = GenerateRingSprite();
        }
    }

    /// <summary>
    /// 程序化生成圆环 Sprite（透明中心 + 白色圆环边缘）
    /// </summary>
    Sprite GenerateRingSprite()
    {
        int size = ringResolution;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size / 2f;
        float outerRadius = center;
        float innerRadius = center * (1f - ringThickness);

        Color white = Color.white;
        Color clear = new Color(1, 1, 1, 0);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float t = Mathf.InverseLerp(innerRadius, outerRadius, dist);
                // 抗锯齿：在内外边缘做 1px 渐变
                float aa = 1f;
                if (dist < innerRadius - 1f)
                    aa = 0f;
                else if (dist < innerRadius + 1f)
                    aa = Mathf.InverseLerp(innerRadius - 1f, innerRadius + 1f, dist);
                else if (dist > outerRadius - 1f)
                    aa = 1f - Mathf.InverseLerp(outerRadius - 1f, outerRadius + 1f, dist);

                tex.SetPixel(x, y, new Color(1, 1, 1, aa));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (shuttlecock == null || playerTransform == null || circleRect == null)
            return;

        float ballDist = Vector3.Distance(playerTransform.position, shuttlecock.transform.position);

        // === 1. 圈大小：球越远越大 ===
        float sizeT = Mathf.Clamp01(ballDist / maxConsiderDistance);
        float targetSize = Mathf.Lerp(minCircleSize, maxCircleSize, sizeT);
        currentSize = Mathf.Lerp(currentSize, targetSize, sizeSmoothSpeed * Time.deltaTime);
        circleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize);
        circleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize);

        // === 2. 圈颜色 ===
        Color targetColor;

        if (ballDist > maxHitDistance)
        {
            // 球太远，无法击中 → 灰色
            targetColor = cantHitColor;
        }
        else
        {
            // 可击中范围，计算离最佳距离有多近
            float distFromOptimal = Mathf.Abs(ballDist - optimalHitDistance);

            if (distFromOptimal <= optimalRange)
            {
                // 在最佳范围内 → 橙色
                targetColor = orangeColor;
            }
            else
            {
                // 在可击中范围内但不在最佳区间
                // 离最佳距离越远 → 越红，越近 → 越绿
                float maxDeviation = maxHitDistance - optimalHitDistance; // 最大偏离
                if (maxDeviation <= 0f) maxDeviation = 1f;
                float t = Mathf.Clamp01((distFromOptimal - optimalRange) / maxDeviation);
                targetColor = Color.Lerp(greenColor, redColor, t);
            }
        }

        currentColor = Color.Lerp(currentColor, targetColor, colorSmoothSpeed * Time.deltaTime);
        circleImage.color = currentColor;
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // 可视化击球距离
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform.position, optimalHitDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerTransform.position, maxHitDistance);
    }
}
