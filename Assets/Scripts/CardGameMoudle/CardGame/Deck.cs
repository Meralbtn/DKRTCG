using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class Deck
    {
        public string _deckName;
        public Dictionary<int,int> _cards;
        public int _maxCardCount = 40;
        public int _count=0;
        public bool _isLegal = false;

        public Deck()
        {
            _count = 0;
            _cards = new Dictionary<int, int>();
            _deckName = "默认卡组";
        }


        public List<Card> DeckToBattlePile()
        {
            var systemCards = PlayerManager.Instance().GetCardList();
            List<Card> pile = new List<Card>();
            foreach (var card in _cards)
            {
                int count = card.Value;
                
                for (int i = 0; i < count; i++)
                {
                    //深拷贝确保不影响系统牌
                    pile.Add(systemCards[card.Key].Clone());
                }
            }
            return pile;
        }

        public List<int> DeckToNetList()
        {
            List<int> netList = new List<int>();
            foreach (var card in _cards)
            {
                netList.Add(card.Key);
            }
            return netList;
        }

        public bool IsLegal()
        {
            GetCardCount();
            if (_count == _maxCardCount)
            {
                _isLegal = true;
                return _isLegal;
            }
            _isLegal = false;
            return _isLegal;
        }
        public int GetCardCount()
        {
            _count = 0;
            foreach (var value in _cards.Values)
            {
                _count += value;
            }
            if (_count == _maxCardCount)
                _isLegal = true;
            Debug.Log(_count);
            return _count;
        }

        public void AddCard(int cardId)
        {
            if (_cards.ContainsKey(cardId))
            {
                // 如果已经有了，就只增加数量
                _cards[cardId]++; 
            }
            else
            {
                // 如果没有，才执行 Add
                _cards.Add(cardId, 1);
            }
        }
        
        public void DeleteCard(int cardId)
        {
            if (!_cards.ContainsKey(cardId))
                return;

            if (_cards[cardId] > 1)
            {
                _cards[cardId]--;
            }
            else
            {
                _cards.Remove(cardId);
            }
        }
    }
}