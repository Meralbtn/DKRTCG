using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;
using Unity.UI;
using TMPro;
using UnityEngine.UI;
using JetBrains.Annotations;

//用来控制卡牌的显示
public class CardUIManager : MonoBehaviour
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
    #endregion
    public void InitialCard()
    {
        if (state == CardState.Battle)
        {
            GetComponent<CardDragHandler>().enabled = false;
            GetComponent<CardInBattleUI>().enabled = true;
        }
        countPoint.gameObject.SetActive(false);
        count.gameObject.SetActive(false);
        cardName.text = card.CardName;
        costText.text = card.Cost.ToString();
        if (card is MinionCard)
        {
            var minion = card as MinionCard;
            attackText.text = minion.Attack.ToString();
            healthText.text = minion.Health.ToString();
        }
        else if (card is SpellCard)
        {
            var spell = card as SpellCard;
            attackText.gameObject.SetActive(false);
            healthText.gameObject.SetActive(false);
        }
        var DragHandler = GetComponent<CardDragHandler>();
        DragHandler.cardId = card.CardID;
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
        count.text = "×"+value.ToString();
        countPoint.gameObject.SetActive(true);
        count.gameObject.SetActive(true);
    }

  
}
