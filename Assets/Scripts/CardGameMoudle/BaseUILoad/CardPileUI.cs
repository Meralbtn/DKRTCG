using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class CardPileUI : MonoBehaviour
    {
        public Transform _father;
        public GameObject _cardPrefab;
        private void Awake()
        {
            Initialized();
        }

        public void CleanUI()
        {
            foreach (Transform child in _father)
            {
                Destroy(child.gameObject);
            }
        }
        //生成卡表,与loadtemp重复，后续可合为一个脚本
        public void Initialized()
        {
            CleanUI();
            //读取字典里的卡片
            //需要补充内容
            List<Card> cards = PlayerManager.Instance().GetCardList();
            foreach (var card in cards)
            {
                GameObject cardUI = Instantiate(_cardPrefab, _father);


                Vector3 ps = _father.lossyScale;
                cardUI.transform.localScale = new Vector3(
                    1f / ps.x,
                    1f / ps.y,
                    1f
                );
                CardUIManager manager = cardUI.GetComponent<CardUIManager>();
                manager.card = card;
                manager.InitialCard();
            }
        }
    }
}