using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardOutlineController : MonoBehaviour
{
    [Header("描边颜色")]
    [SerializeField] private Color availableColor = new Color(0.2f, 1f, 0.3f);  // 绿色可用
    [SerializeField] private Color selectedColor  = new Color(0f, 0.8f, 1f);    // 蓝色已选中

    [Header("呼吸动效（Available状态）")]
    [SerializeField] private float pulseMinAlpha = 0.4f;
    [SerializeField] private float pulseMaxAlpha = 1f;
    [SerializeField] private float pulseDuration = 0.7f;

    // 拖拽到卡牌Prefab上的边框Image（套在卡牌外层，比卡牌本体略大）
    [SerializeField] public Image outlineImage;

    private Tween _pulseTween;

    public void SetOutline(OutlineState state)
    {
        if (outlineImage == null) return;

        StopPulse();

        switch (state)
        {
            case OutlineState.None:
                outlineImage.enabled = false;
                break;

            case OutlineState.Available:
                outlineImage.enabled = true;
                outlineImage.color = availableColor;
                StartPulse();
                break;

            case OutlineState.Selected:
                outlineImage.enabled = true;
                outlineImage.color = selectedColor;
                break;
        }
    }

    private void StartPulse()
    {
        Color c = outlineImage.color;
        outlineImage.color = new Color(c.r, c.g, c.b, pulseMaxAlpha);
        _pulseTween = outlineImage
            .DOFade(pulseMinAlpha, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StopPulse()
    {
        _pulseTween?.Kill();
        _pulseTween = null;
        if (outlineImage != null)
        {
            Color c = outlineImage.color;
            outlineImage.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    private void OnDestroy()
    {
        _pulseTween?.Kill();
    }
}

public enum OutlineState { None, Available, Selected }