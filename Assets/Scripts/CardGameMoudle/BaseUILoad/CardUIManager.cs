using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;
using UnityEngine.EventSystems;

//用来控制卡牌的显示
public class CardUIManager : MonoBehaviour, IPointerClickHandler
{
    //UI值
    #region variable
    public TextMeshProUGUI cardName;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI healthText;
    public Image background;
    public Image countPoint;
    public Card card;
    public TextMeshProUGUI count;
    public CardState state;
    public GameObject _cardBack;
    public GameObject _cardFront;
    public GameObject _attackIcon;
    public GameObject _healthIcon;
    #endregion
    public void InitialCard()
    {
        // 对象池复用时，重置可能被溶解效果改掉的 alpha
        ResetTextAlpha();

        _cardFront.SetActive(true);
        _cardBack.SetActive(false);
        _attackIcon.gameObject.SetActive(true);
        _healthIcon.gameObject.SetActive(true);
        attackText.gameObject.SetActive(true);
        healthText.gameObject.SetActive(true);
        if (state == CardState.Battle)
        {
            GetComponent<CardDragHandler>().enabled = false;
            GetComponent<CardInBattleUI>().enabled = true;
        }
        countPoint.gameObject.SetActive(false);
        count.gameObject.SetActive(false);
        cardName.text = card.CardName;
        costText.text = card.Cost.ToString();
        costText.color = Color.white;
        if (card is MinionCard)
        {
            var minion = card as MinionCard;
            attackText.text = minion.Attack.ToString();
            healthText.text = minion.Health.ToString();
        }
        else if (card is SpellCard)
        {
            _attackIcon.gameObject.SetActive(false);
            _healthIcon.gameObject.SetActive(false);
            attackText.gameObject.SetActive(false);
            healthText.gameObject.SetActive(false);
        }
        var DragHandler = GetComponent<CardDragHandler>();
        DragHandler.cardId = card.CardID;
        LoadCardImage(card.CardID);
    }
    private void LoadCardImage(int cardId)
    {
        // 路径格式: Assets/Resources/CardImage/Card/0
        Sprite sprite = Resources.Load<Sprite>($"CardImage/Card/{cardId}");

        if (sprite != null)
        {
            background.sprite = sprite;
           
        }
        else
        {
            Debug.LogWarning($"找不到卡图: CardImage/Card/{cardId}");
        }
    }

    private void FillMask(Sprite sprite)
    {
        // 获取 Mask 容器尺寸（250*350）
        RectTransform maskRect = background.transform.parent.GetComponent<RectTransform>();
        float maskW = maskRect.rect.width;   // 250
        float maskH = maskRect.rect.height;  // 350

        // 计算图片原始比例
        float spriteW = sprite.rect.width;
        float spriteH = sprite.rect.height;

        // 按"填满"缩放（类似 CSS background-size: cover）
        float scale = Mathf.Max(maskW / spriteW, maskH / spriteH);

        RectTransform rt = background.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(spriteW * scale, spriteH * scale);
        rt.anchoredPosition = Vector2.zero; // 居中
    }

    //渲染卡背
    public void InitialBackCard()
    {
        if (state == CardState.Battle)
        {
            GetComponent<CardDragHandler>().enabled = false;
            GetComponent<CardInBattleUI>().enabled = true;

        }
        countPoint.gameObject.SetActive(false);
        count.gameObject.SetActive(false);
        _cardFront.SetActive(false);
        _cardBack.SetActive(true);
    }

    //可被state优化
    public void ShowDeckNum(int value)
    {
        count.text = "×" + value.ToString();
        countPoint.gameObject.SetActive(true);
        count.gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (card == null) return;
        if (CardDetailPanel.Instance == null) return;
        CardDetailPanel.Instance.Show(card);
    }

    private void ResetTextAlpha()
    {
        foreach (var t in new TMP_Text[] { cardName, attackText, costText, healthText, count })
        {
            if (t == null) continue;
            var c = t.color;
            t.color = new Color(c.r, c.g, c.b, 1f);
        }
    }
}
