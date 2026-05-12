using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CardFrameApplier : MonoBehaviour
{
    [SerializeField] private Material _frameMaterial;

    private Material _instance;
    // 在 Apply() 执行前缓存待设置的颜色，避免时序问题
    private Color _pendingBorderColor = new Color(0f, 0.9f, 1f, 1f);

    private IEnumerator Start()
    {
        yield return null; // 等一帧，让 UI 布局计算完毕再读 rect.size
        Apply();
    }

    public void Apply()
    {
        if (_frameMaterial == null) return;

        var img = GetComponent<Image>();
        var rt  = GetComponent<RectTransform>();

        _instance = new Material(_frameMaterial);
        _instance.SetVector("_CardSize",
            new Vector4(rt.rect.width, rt.rect.height, 0, 0));
        _instance.SetColor("_BorderColor", _pendingBorderColor);
        img.material = _instance;
    }

    public float GlowIntensity
    {
        get => _instance != null ? _instance.GetFloat("_GlowIntensity") : 1f;
        set { if (_instance != null) _instance.SetFloat("_GlowIntensity", value); }
    }

    public Color BorderColor
    {
        get => _pendingBorderColor;
        set
        {
            _pendingBorderColor = value;
            if (_instance != null) _instance.SetColor("_BorderColor", value);
        }
    }

    private void OnDestroy()
    {
        if (_instance != null)
            Destroy(_instance);
    }
}
