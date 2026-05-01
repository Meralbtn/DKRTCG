using System;
using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;

public class LoadTempDeck : MonoBehaviour
{
    public GameObject _cardPrefab;
    public Transform _father;

    private void Awake()
    {
        LoadTempDeckUI();
    }
    public void CleanUI()
    {
        foreach (Transform child in _father)
        {
            Destroy(child.gameObject);
        }
    }
    //加载临时牌组
    public void LoadTempDeckUI()
    {
        CleanUI();
        Deck deck = PlayerManager.Instance().GetTempDeck();
        var cards = deck._cards;
        //读取字典里的卡片
        //需要补充内容
        foreach (var value in cards)
        {
            List<Card> systemCard = PlayerManager.Instance().GetCardList();
            Card card = systemCard[value.Key];
            GameObject cardUI = Instantiate(_cardPrefab, _father);
            CardUIManager manager =cardUI.GetComponent<CardUIManager>();
            manager.card = card;
            manager.InitialCard();
            manager.ShowDeckNum(value.Value);
        }
    }
    
}
