using System;
using UnityEngine;
using Unity.UI;
using TMPro;
namespace CardGame
{
    public class PlayerDataHandler : MonoBehaviour
    {
        public int _cost = 0;
        public int _totalCost = 10;
        public int _playerHealth = 25;
        public TextMeshProUGUI _costText;
        public TextMeshProUGUI _playerHealthText;
        public Deck _playerDeck;
        private void Start()
        {
           
        }

        private void Awake()
        {
            
        }
    }
}