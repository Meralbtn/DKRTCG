using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Button))]
public class PressStartButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("颜色")]
    public Color bgColor       = new Color(0.06f, 0.06f, 0.12f, 1f);
    public Color borderNormal  = new Color(0.25f, 0.25f, 0.35f, 1f);
    public Color borderHover   = new Color(0f,    1f,    0.53f, 1f);
    public Color borderPressed = new Color(0f,    0.7f,  0.38f, 1f);
    public Color textColor     = new Color(0.85f, 0.85f, 0.95f, 1f);

    [Header("尺寸")]
    public int borderWidth = 2;

    [Header("内容")]
    public string label = "PRESS START";

    Button          _btn;
    Image           _bg;
    Image           _top, _bottom, _left, _right;
    TextMeshProUGUI _label;
    Image           _icon;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.transition = Selectable.Transition.None;
        BuildUI();
    }

    void BuildUI()
    {
        // 1. 背景
        _bg = MakeImg("BG", transform);
        _bg.color = bgColor;
        Stretch(_bg.rectTransform, 0, 0, 0, 0);

        // 2. 四条边框（sprite = null 避免蓝色圆角默认图）
        _top    = MakeEdge("B_Top",    0,           -borderWidth, 0,            0           );
        _bottom = MakeEdge("B_Bottom", -borderWidth, 0,           0,            0           );
        _left   = MakeEdge("B_Left",   0,            0,           0,           -borderWidth );
        _right  = MakeEdge("B_Right",  0,            0,          -borderWidth,  0           );
        SetBorderColor(borderNormal);

        // 3. 内容行
        var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(transform, false);
        Stretch(row.GetComponent<RectTransform>(), 0, 0, 0, 0);
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.spacing                = 10;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(20, 20, 0, 0);

        // 4. ▶ 三角图标
        var iconGO = new GameObject("Icon", typeof(Image));
        iconGO.transform.SetParent(row.transform, false);
        _icon = iconGO.GetComponent<Image>();
        _icon.sprite        = MakeTriangleSprite(10, 12);
        _icon.color         = textColor;
        _icon.raycastTarget = false;
        _icon.preserveAspect = true;
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(10, 12);
        var iconLE = iconGO.AddComponent<LayoutElement>();
        iconLE.preferredWidth  = 10;
        iconLE.preferredHeight = 12;

        // 5. 文字
        var txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(row.transform, false);
        _label = txtGO.GetComponent<TextMeshProUGUI>();
        _label.text               = label;
        _label.fontSize           = 13;
        _label.color              = textColor;
        _label.alignment          = TextAlignmentOptions.MidlineLeft;
        _label.raycastTarget      = false;
        _label.fontStyle          = FontStyles.Bold;
        _label.characterSpacing   = 4;
        _label.enableWordWrapping = false;   // ← 禁止换行
        _label.overflowMode       = TextOverflowModes.Overflow;
        var txtLE = txtGO.AddComponent<LayoutElement>();
        txtLE.preferredHeight = 48;
    }

    // 生成右向三角 Sprite（程序生成，无需美术资源）
    Sprite MakeTriangleSprite(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color32[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // 右向三角：以中心 y 为基准，x 在三角范围内则填充
            float half = h * 0.5f;
            float dist = Mathf.Abs(y - (half - 0.5f)); // 距中心距离
            float limit = (half - dist) / half * w;     // 该行最大 x
            px[y * w + x] = (x < limit)
                ? new Color32(255, 255, 255, 255)
                : new Color32(0, 0, 0, 0);
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 10);
    }

    // ── 工具方法 ─────────────────────────────────────────────
    Image MakeEdge(string n, float t, float b, float l, float r)
    {
        var img = MakeImg(n, transform);
        img.sprite = null;          // ← 清除默认圆角 sprite，避免蓝色角点
        Stretch(img.rectTransform, t, b, l, r);
        return img;
    }

    Image MakeImg(string n, Transform parent)
    {
        var go = new GameObject(n, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = null;
        return img;
    }

    void Stretch(RectTransform rt, float t, float b, float l, float r)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    void SetBorderColor(Color c)
    {
        if (_top)    _top.color    = c;
        if (_bottom) _bottom.color = c;
        if (_left)   _left.color   = c;
        if (_right)  _right.color  = c;
    }

    // ── 交互 ─────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData _)
    {
        SetBorderColor(borderHover);
        _icon.color  = borderHover;
        _label.color = Color.white;
    }

    public void OnPointerExit(PointerEventData _)
    {
        SetBorderColor(borderNormal);
        _icon.color  = textColor;
        _label.color = textColor;
    }

    public void OnPointerDown(PointerEventData _)
    {
        SetBorderColor(borderPressed);
        GetComponent<RectTransform>().anchoredPosition += new Vector2(2, -2);
        StartCoroutine(FlashBg());
    }

    public void OnPointerUp(PointerEventData _)
    {
        SetBorderColor(borderHover);
        GetComponent<RectTransform>().anchoredPosition -= new Vector2(2, -2);
    }

    IEnumerator FlashBg()
    {
        _bg.color = new Color(0f, 1f, 0.53f, 0.15f);
        yield return new WaitForSeconds(0.08f);
        _bg.color = bgColor;
    }
}