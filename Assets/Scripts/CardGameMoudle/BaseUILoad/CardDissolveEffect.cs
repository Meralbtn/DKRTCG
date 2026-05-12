using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class CardDissolveEffect : MonoBehaviour
{
    [Header("溶解参数")]
    [SerializeField] private Shader dissolveShader;
    [SerializeField] private Color edgeColor = new Color(0.25f, 1f, 0.4f);
    [SerializeField] private float edgeWidth = 0.06f;
    [SerializeField] private float dissolveDuration = 0.55f;
    [SerializeField] private float floatUpAmount = 60f;

    private static readonly int PropAmount    = Shader.PropertyToID("_DissolveAmount");
    private static readonly int PropEdgeColor = Shader.PropertyToID("_EdgeColor");
    private static readonly int PropEdgeWidth = Shader.PropertyToID("_EdgeWidth");

    private readonly List<(Image img, Material mat)> _imgMats = new();

    public void PlayDissolve(Action onComplete)
    {
        if (dissolveShader == null)
        {
            // 降级：直接回调
            onComplete?.Invoke();
            return;
        }

        // 给所有子 Image 创建独立材质实例
        foreach (var img in GetComponentsInChildren<Image>(true))
        {
            var mat = new Material(dissolveShader);
            mat.SetColor(PropEdgeColor, edgeColor);
            mat.SetFloat(PropEdgeWidth, edgeWidth);
            mat.SetFloat(PropAmount, 0f);
            img.material = mat;
            _imgMats.Add((img, mat));
        }

        float dissolveVal = 0f;

        var seq = DOTween.Sequence();

        // 轻微放大冲击感（0.1s）→ 溶解上浮（dissolveDuration）
        seq.Append(transform.DOScale(transform.localScale * 1.08f, 0.1f).SetEase(Ease.OutBack));

        seq.Append(
            DOTween.To(() => dissolveVal, v =>
            {
                dissolveVal = v;
                foreach (var (_, mat) in _imgMats)
                    mat.SetFloat(PropAmount, v);
            }, 1f, dissolveDuration).SetEase(Ease.InQuad)
        );

        // 上浮与溶解同步
        seq.Join(transform.DOLocalMoveY(transform.localPosition.y + floatUpAmount, dissolveDuration)
            .SetEase(Ease.OutQuad));

        // TMP 文字淡出（比溶解稍快，避免文字悬空）
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            seq.Join(tmp.DOFade(0f, dissolveDuration * 0.5f));

        seq.OnComplete(() =>
        {
            // 还原材质 & 释放材质内存
            foreach (var (img, mat) in _imgMats)
            {
                if (img != null) img.material = null;
                Destroy(mat);
            }
            _imgMats.Clear();

            // 重置文字 alpha，避免对象池复用时文字透明
            foreach (var tmp in GetComponentsInChildren<TMP_Text>(true))
            {
                var c = tmp.color;
                tmp.color = new Color(c.r, c.g, c.b, 1f);
            }

            onComplete?.Invoke();
        });
    }

    private void OnDestroy()
    {
        foreach (var (_, mat) in _imgMats)
            if (mat != null) Destroy(mat);
    }
}
