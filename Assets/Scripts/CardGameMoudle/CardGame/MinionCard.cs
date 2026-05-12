namespace CardGame
{
    public class MinionCard : Card
    {
        //将随从逻辑载入
        public int Attack { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool Charge { get; set; }
        public int AttackCount { get; set; }
        public string EffectType { get; set; }
        public int EffectValue { get; set; }

        public MinionCard(int id, string name, int cost, int attack, int health,
            string effectType = "None", int effectValue = 0, string description = "")
        {
            CardID = id;
            CardName = name;
            Cost = cost;
            Attack = attack;
            Health = health;
            MaxHealth = health;
            Type = CardType.Minion;
            AttackCount = 1;
            EffectType = effectType;
            EffectValue = effectValue;
            Description = description;
        }

        public override Card Clone()
        {
            return new MinionCard(CardID, CardName, Cost, Attack, Health, EffectType, EffectValue, Description);
        }

    }
}