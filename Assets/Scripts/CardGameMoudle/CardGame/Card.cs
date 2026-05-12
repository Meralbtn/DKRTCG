using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    //卡牌基类
    public abstract class Card
    {
        #region variable

        public bool IsBack;
        public int CardID { get; set; }
        public string CardName{ get; set; }
        public int Cost{ get; set; }
        public CardType Type{ get; set; }
        public string Description { get; set; } 
        //public Player Owner{ get;set; }
        #endregion

        #region func
        public virtual void OnPlay()
        {
            
        }

        public virtual Card Clone()
        {
            return null;
        }

        #endregion
        
    }
}