namespace CardGame
{
    public class MinionCard : Card
    {
        //将随从逻辑载入
        public int Attack { get; set; }
        public int Health{ get; set; }
        public int MaxHealth{ get; set; }
        public bool Charge { get; set; }
        public int AttackCount { get; set; }    
        //初始化数据
        public MinionCard(int id, string name, int cost, int attack, int health)
        {
            CardID = id;
            CardName = name;
            Cost = cost;
            Attack = attack;
            Health = health;
            Type = CardType.Minion;
            AttackCount = 1;
        }
        public override void OnPlay()
        {
            
        }

        public override Card Clone()
        {
            MinionCard card = new MinionCard(CardID, CardName, Cost, Attack, Health);
            return card;
        }
        
    }
}