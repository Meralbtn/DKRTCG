namespace CardGame
{
    public class SpellCard : Card
    {
        public string Effect;
   
        public SpellCard(int id, string name, int cost,string effect)
        {
            CardID = id;
            CardName = name;
            Cost = cost;
            Type = CardType.Spell;
            Effect = effect;
        }
    }
}