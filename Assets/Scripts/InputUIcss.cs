using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PixelInputField : MonoBehaviour
{
    [Header("外观")]
    public Color bgColor         = new Color(0.08f, 0.08f, 0.15f, 1f);  // #141428
    public Color borderNormal    = new Color(0.20f, 0.20f, 0.30f, 1f);  // 暗边框
    public Color borderFocus     = new Color(0f,    1f,    0.53f, 1f);  // #00FF88
    public Color textColor       = new Color(0.90f, 0.90f, 0.95f, 1f);  // 近白
    public Color placeholderColor= new Color(0.35f, 0.35f, 0.45f, 1f);  // 灰
    public Color caretColor      = new Color(0f,    1f,    0.53f, 1f);  // 绿色光标
    public int   cornerRadius    = 8;
    public int   borderWidth     = 1;

    [Header("内容")]
    public string placeholder = "Enter username...";
    public bool   isPassword  = false;
    public TMP_FontAsset pixelFont;

    public TMP_InputField _input;
    Image          _bg;
    Image          _border;

    void Awake() => Build();

    void Build()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) rt = gameObject.AddComponent<RectTransform>();

        // 1. 外边框层（比背景大 borderWidth）
        _border = MakeImg("Border", transform, MakeRoundedSprite(cornerRadius));
        _border.color = borderNormal;
        Stretch(_border.rectTransform, 0, 0, 0, 0);

        // 2. 背景层（内缩 borderWidth）
        _bg = MakeImg("BG", transform, MakeRoundedSprite(cornerRadius - 1));
        _bg.color = bgColor;
        int bw = borderWidth;
        Stretch(_bg.rectTransform, bw, bw, bw, bw);

        // 3. TMP_InputField
        var inputGO = new GameObject("InputField", typeof(RectTransform));
        inputGO.transform.SetParent(transform, false);
        Stretch(inputGO.GetComponent<RectTransform>(), 0, 0, 0, 0);

        _input = inputGO.AddComponent<TMP_InputField>();

        // 4. Text Area（输入文字的区域）
        var areaGO = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        areaGO.transform.SetParent(inputGO.transform, false);
        var areaRT = areaGO.GetComponent<RectTransform>();
        Stretch(areaRT, 0, 0, 0, 0);
        areaRT.offsetMin = new Vector2(16, 8);   // 内边距
        areaRT.offsetMax = new Vector2(-16, -8);

        // 5. Placeholder
        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(areaGO.transform, false);
        var ph = phGO.GetComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.color     = placeholderColor;
        ph.fontSize  = 14;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        ph.enableWordWrapping = false;
        if (pixelFont) ph.font = pixelFont;
        Stretch(ph.rectTransform, 0, 0, 0, 0);

        // 6. 输入文字
        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(areaGO.transform, false);
        var txt = txtGO.GetComponent<TextMeshProUGUI>();
        txt.text      = "";
        txt.color     = textColor;
        txt.fontSize  = 14;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.enableWordWrapping = false;
        if (pixelFont) txt.font = pixelFont;
        Stretch(txt.rectTransform, 0, 0, 0, 0);

        // 7. 绑定 TMP_InputField
        _input.textViewport   = areaRT;
        _input.textComponent  = txt;
        _input.placeholder    = ph;
        _input.caretColor     = caretColor;
        _input.caretWidth     = 3;
        _input.customCaretColor = true;
        _input.selectionColor = new Color(0f, 1f, 0.53f, 0.3f);

        if (isPassword)
        {
            _input.contentType = TMP_InputField.ContentType.Password;
            _input.asteriskChar = '●';
        }

        // 8. 监听 Focus / Blur → 切换边框颜色
        _input.onSelect.AddListener(_   => _border.color = borderFocus);
        _input.onDeselect.AddListener(_ => _border.color = borderNormal);
    }

    // ── 生成圆角 Sprite（程序生成）────────────────────────────
    Sprite MakeRoundedSprite(int radius)
    {
        int size = 64;
        radius = Mathf.Clamp(radius, 1, size / 2);
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            // 四个角的圆心
            float cx = (fx < radius) ? radius : (fx > size - radius) ? size - radius : fx;
            float cy = (fy < radius) ? radius : (fy > size - radius) ? size - radius : fy;
            float dist = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
            byte a = (byte)(dist <= radius ? 255 : 0);
            px[y * size + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px);
        tex.Apply();

        int b = size / 4;
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100,
            0, SpriteMeshType.FullRect,
            new Vector4(b, b, b, b));  // 9-slice border
    }

    Image MakeImg(string n, Transform parent, Sprite sprite = null)
    {
        var go  = new GameObject(n, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite    = sprite;
        img.type      = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = (n == "InputField" || n == "Border");
        return img;
    }

    void Stretch(RectTransform rt, float t, float b, float l, float r)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    // 外部获取输入内容
    public string GetText() => _input.text;
    public void   SetText(string t) => _input.text = t;
}