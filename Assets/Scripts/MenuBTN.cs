using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// 挂在任意 Button 物体上，自动生成中世纪像素风 3D 凸起效果。
/// 无需手动搭层级，Inspector 里调色即可。
/// </summary>
[RequireComponent(typeof(Button))]
public class MedievalButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("主题色（选一个预设或自定义）")]
    public ButtonTheme theme = ButtonTheme.Gold;
    [Header("自定义颜色（Theme 选 Custom 时生效）")]
    public Color customTop    = new Color(0.55f, 0.37f, 0.10f);
    public Color customShadow = new Color(0.23f, 0.13f, 0.03f);
    public Color customText   = new Color(1f,    0.96f, 0.84f);
    public Color customHighlight = new Color(0.78f, 0.57f, 0.16f);

    [Header("设置")]
    public string buttonText  = "登录";
    public int    pressDepth  = 4;      // 按下下沉像素数
    public TMP_FontAsset font;          // 可选，拖入像素字体

    // ── 预设主题 ──────────────────────────────────────────────
    public enum ButtonTheme { Gold, Green, Blue, Red, Custom }

    struct ThemeData
    {
        public Color top, shadow, text, highlight;
    }

    ThemeData GetTheme()
    {
        switch (theme)
        {
            case ButtonTheme.Green:  return new ThemeData { top = HC("1e4d1e"), shadow = HC("0d2e0d"), text = HC("c8ffc8"), highlight = HC("3a8a3a") };
            case ButtonTheme.Blue:   return new ThemeData { top = HC("1e1e5a"), shadow = HC("0a0a2e"), text = HC("d0d0ff"), highlight = HC("5a5acc") };
            case ButtonTheme.Red:    return new ThemeData { top = HC("5a1e1e"), shadow = HC("2e0a0a"), text = HC("ffd0d0"), highlight = HC("cc5a5a") };
            case ButtonTheme.Custom: return new ThemeData { top = customTop,   shadow = customShadow, text = customText,   highlight = customHighlight };
            default:                 return new ThemeData { top = HC("8B5E1A"), shadow = HC("3a2008"), text = HC("fff5d6"), highlight = HC("c8922a") }; // Gold
        }
    }

    // ── 运行时引用 ────────────────────────────────────────────
    Button          _btn;
    RectTransform   _topRT;     // 顶层（按下时下移）
    Image           _topImg;
    Image           _shadowImg;
    Image           _hlImg;     // 左上高光线
    TextMeshProUGUI _label;

    bool _isPressed;

    // ── 生命周期 ──────────────────────────────────────────────
    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.transition = Selectable.Transition.None;

        // 清理 Button 自带的 Image，避免白色背景
        var selfImg = GetComponent<Image>();
        if (selfImg) { selfImg.color = Color.clear; selfImg.raycastTarget = true; }

        Build();
    }

    void Build()
    {
        var d = GetTheme();

        // 1. 阴影层（底部偏移，最先渲染在最下面）
        var shadowGO = new GameObject("Shadow", typeof(Image));
        shadowGO.transform.SetParent(transform, false);
        _shadowImg = shadowGO.GetComponent<Image>();
        _shadowImg.sprite = null;
        _shadowImg.color  = d.shadow;
        _shadowImg.raycastTarget = false;
        var shadowRT = shadowGO.GetComponent<RectTransform>();
        Stretch(shadowRT, pressDepth, -pressDepth, 0, 0); // 整体下移 pressDepth

        // 2. 顶层容器（主色 + 边框 + 高光）
        var topGO = new GameObject("Top", typeof(RectTransform));
        topGO.transform.SetParent(transform, false);
        _topRT = topGO.GetComponent<RectTransform>();
        Stretch(_topRT, 0, pressDepth, 0, 0);             // 底部留出阴影空间

        // 2a. 主色背景
        _topImg = topGO.AddComponent<Image>();
        _topImg.sprite = null;
        _topImg.color  = d.top;
        _topImg.raycastTarget = false;

        // 2b. 外边框（子物体盖在主色上）
        MakeBorder(topGO.transform, d.shadow);

        // 2c. 左上高光线（内侧亮边，模拟像素立体感）
        _hlImg = MakeHighlight(topGO.transform, d.highlight);

        // 3. 文字
        var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(_topRT, false);
        _label = txtGO.GetComponent<TextMeshProUGUI>();
        _label.text               = buttonText;
        _label.color              = d.text;
        _label.fontSize           = 18;
        _label.alignment          = TextAlignmentOptions.Midline;
        _label.enableWordWrapping = false;
        _label.raycastTarget      = false;
        if (font) _label.font = font;
        Stretch(_label.rectTransform, 0, 0, 0, 0);
    }

    // 四条外边框（用 2px 的细条 Image 拼成）
    void MakeBorder(Transform parent, Color c)
    {
        int bw = 2;
        MakeRect("BT", parent, c, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-bw),  new Vector2(0,0));
        MakeRect("BB", parent, c, new Vector2(0,0), new Vector2(1,0), new Vector2(0,0),    new Vector2(0,bw));
        MakeRect("BL", parent, c, new Vector2(0,0), new Vector2(0,1), new Vector2(0,0),    new Vector2(bw,0));
        MakeRect("BR", parent, c, new Vector2(1,0), new Vector2(1,1), new Vector2(-bw,0),  new Vector2(0,0));
    }

    // 左上高光（内侧 1px 亮线）
    Image MakeHighlight(Transform parent, Color c)
    {
        // 左边
        MakeRect("HLLeft",  parent, c, new Vector2(0,0), new Vector2(0,1), new Vector2(2,2),  new Vector2(3,-2));
        // 上边
        var img = MakeRect("HLTop", parent, c, new Vector2(0,1), new Vector2(1,1), new Vector2(2,-3), new Vector2(-2,1));
        return img;
    }

    Image MakeRect(string n, Transform parent, Color c, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go  = new GameObject(n, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = null;
        img.color  = c;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return img;
    }

    void Stretch(RectTransform rt, float t, float b, float l, float r)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    // ── 交互 ─────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData _)
    {
        if (!_isPressed) StartCoroutine(TintTop(1.2f));
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (!_isPressed) StartCoroutine(TintTop(1f));
    }

    public void OnPointerDown(PointerEventData _)
    {
        _isPressed = true;
        // 顶层下移，露出阴影，模拟按下
        _topRT.offsetMin = new Vector2(0, 0);
        _topRT.offsetMax = new Vector2(0, -(pressDepth));
        _shadowImg.color = Color.clear;  // 按下时隐藏阴影（已被顶层覆盖）
    }

    public void OnPointerUp(PointerEventData _)
    {
        _isPressed = false;
        _topRT.offsetMin = new Vector2(0, pressDepth);
        _topRT.offsetMax = new Vector2(0, 0);
        _shadowImg.color = GetTheme().shadow;
    }

    IEnumerator TintTop(float brightness)
    {
        var baseColor = GetTheme().top;
        _topImg.color = new Color(
            Mathf.Clamp01(baseColor.r * brightness),
            Mathf.Clamp01(baseColor.g * brightness),
            Mathf.Clamp01(baseColor.b * brightness)
        );
        yield break;
    }

    // 十六进制转 Color
    static Color HC(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    // ── Editor 下实时预览（可选）────────────────────────────
#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        // 运行时修改 Inspector 值时刷新颜色
        var d = GetTheme();
        if (_topImg)    _topImg.color    = d.top;
        if (_shadowImg) _shadowImg.color = d.shadow;
        if (_label)   { _label.text = buttonText; _label.color = d.text; }
    }
#endif
}