namespace CardGame
{
    public class SpellCard : Card
    {
        public string Effect;
        public string EffectType;
        public int EffectValue;
        //需要选目标
        public bool NeedsTarget; 
        public SpellCard(int id, string name, int cost, string effect, string effectType = "None", int effectValue = 0, bool needsTarget = false, string description = "")
        {
            CardID = id;
            CardName = name;
            Cost = cost;
            Type = CardType.Spell;
            Effect = effect;
            EffectType = effectType;
            EffectValue = effectValue;
            NeedsTarget = needsTarget;
            Description = description;
        }
    }
}