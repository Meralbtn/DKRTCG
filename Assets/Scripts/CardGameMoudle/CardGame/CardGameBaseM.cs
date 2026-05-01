using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public enum CardType
    {
        //定义卡牌类型，随从，法术。武器，场地
        //暂时不考虑Skill系统
        Minion,Spell,Weapon,Scene
    }

    public enum CardState
    {
        Deck, Pile, Battle
    }

    public enum BattleCardState
    {
        Normal, Attacking, BeingAttacked, Dead,Active
    }

    public enum GameState
    { 
        GameStart,PlayerDraw,PlayerTurn,EnemyDraw,EnemyTurn,Win,Lose
    }

    public enum BattleInfo
    {
        First,Last
    }
    
    
}