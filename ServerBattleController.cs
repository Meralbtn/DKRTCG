using System.Text;
using System.Text.Json;
using CardGameApp;
using Newtonsoft.Json;

namespace CardGameServer
{
    public class CardInstance
    {
        public int _instanceId { get; set; }
        public int _cardId { get; set; }
        public int _currentHealth { get; set; }
        public int _currentAttack { get; set; }
        public int _attackUsed { get; set; } = 0;
        public bool _canAttack { get; set; } = false;

        public CardInstance(int instanceId, int cardId, int hp, int atk)
        {
            _instanceId = instanceId;
            _cardId = cardId;
            _currentHealth = hp;
            _currentAttack = atk;
        }
    }

    public class BattleStartPackage
    {
        public string YourRole { get; set; }
        public string EnemyName { get; set; }
        public int MyMaxHealth { get; set; }
        public int EnemyMaxHealth { get; set; }
        public int MyInitialCost { get; set; }
        public int EnemyInitialCost { get; set; }
        public List<HandCardState> MyHand { get; set; }
        public int EnemyHand { get; set; }
        public int MyDeckCount { get; set; }    // 新增
        public int EnemyDeckCount { get; set; } // 新增
    }

    public class UseCardPackage
    {
        public Guid SessionId { get; set; }
        public string ActionId { get; set; }
        public int CardInstanceId { get; set; }
    }

    public class AttackPackage
    {
        public Guid SessionId { get; set; }
        public string ActionId { get; set; }
        public int AttackerInstanceId { get; set; }
        public int TargetInstanceId { get; set; }
    }

    public class EndTurnPackage
    {
        public Guid SessionId { get; set; }
        public string ActionId { get; set; }
    }

    public class ActionAck
    {
        public string ActionId { get; set; }
        public ErrorCode ErrorCode { get; set; }
        public string Reason { get; set; }
    }

    public class BattleState
    {
        public int MyHP, EnemyHP;
        public int MyCost, EnemyCost;
        public int MyMaxCost, EnemyMaxCost;
        public bool IsMyTurn;
        public string GameResult;

        public List<MiniCardState> MyField;
        public List<MiniCardState> EnemyField;
        public List<HandCardState> MyHand;
        public int EnemyHandCount;
        public int MyDeckCount;    // 新增
        public int EnemyDeckCount; // 新增
        public int LastAttackerInstanceId = -1;
        public int LastTargetInstanceId = -1;
    }

    public class MiniCardState
    {
        public int InstanceId;
        public int CardId;
        public int HP, Attack;
        public int AttackUsed;
        public bool CanAttack;
    }

    public class HandCardState
    {
        public int InstanceId;
        public int CardId;
    }

    public class HandCard
    {
        public int InstanceId;
        public int CardId;
    }

    public class BattleController
    {
        public PlayerBaseData Player1;
        public PlayerBaseData Player2;
        public PlayerBaseData CurrentTurnPlayer;
        public bool IsFinished { get; private set; } = false;
        public int P1Health = 25;
        public int P2Health = 25;
        public int P1Cost = 0;
        public int P2Cost = 0;
        private int _handCardCounter = 0;
        private int _cardInstanceCounter = 0;

        public class PlayerBattleState
        {
            public int Hp = 25;
            public int MaxCost = 0;
            public int CurrentCost = 0;
            public List<int> Deck = new();
            public List<HandCard> Hand = new();
            public List<CardInstance> Board = new();
        }

        public Dictionary<string, PlayerBattleState> States = new();

        public BattleController(PlayerBaseData p1, PlayerBaseData p2)
        {
            Player1 = p1;
            Player2 = p2;
            if (p1._deck == null || p2._deck == null)
                Console.WriteLine("玩家卡组不能为空，无法开始战斗");

            States[p1._id] = new PlayerBattleState { Deck = new List<int>(p1._deck) };
            States[p2._id] = new PlayerBattleState { Deck = new List<int>(p2._deck) };
            Shuffle(States[p1._id].Deck);
            Shuffle(States[p2._id].Deck);
        }

        private void Shuffle(List<int> deck)
        {
            Random rng = new Random();
            int n = deck.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = deck[k];
                deck[k] = deck[n];
                deck[n] = value;
            }
        }

        public void StartBattle()
        {
            Console.WriteLine($"战斗开始: {Player1._name} vs {Player2._name}");
            WhoIsFirst();
            var firstId = CurrentTurnPlayer._id;
            var secondId = firstId == Player1._id ? Player2._id : Player1._id;

            States[firstId].MaxCost = 1;
            States[firstId].CurrentCost = 1;
            States[secondId].MaxCost = 0;
            States[secondId].CurrentCost = 0;

            DrawCard(States[Player1._id], 4);
            DrawCard(States[Player2._id], 5);

            SendStartPackage(Player1);
            SendStartPackage(Player2);
        }

        private void SendStartPackage(PlayerBaseData player)
        {
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];
            var isFirst = CurrentTurnPlayer._id == player._id;

            var pkg = new BattleStartPackage
            {
                YourRole = isFirst ? "First" : "Last",
                EnemyName = enemyData._name,
                MyMaxHealth = myState.Hp,
                EnemyMaxHealth = enemyState.Hp,
                MyInitialCost = myState.CurrentCost,
                EnemyInitialCost = enemyState.CurrentCost,
                MyHand = myState.Hand.Select(h => new HandCardState
                {
                    CardId = h.CardId,
                    InstanceId = h.InstanceId
                }).ToList(),
                EnemyHand = enemyState.Hand.Count,
                MyDeckCount = myState.Deck.Count,    // 新增
                EnemyDeckCount = enemyState.Deck.Count, // 新增
            };

            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pkg));
            using var netPkg = new NetPackage();
            netPkg.WriteInt((int)SessionMessageID.BattleStartResult);
            netPkg.WriteInt(data.Length);
            netPkg.WriteBytes(data);
            CardLogic.instance.SendToPlayer(player, netPkg);
        }

        public void HandleUseCard(PlayerBaseData player, UseCardPackage req)
        {
            var myState = States[player._id];

            if (CurrentTurnPlayer._id != player._id)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "还没到你的回合"); return; }

            int handIndex = req.CardInstanceId;
            if (handIndex < 0 || handIndex >= myState.Hand.Count)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "手牌索引无效"); return; }

            int cardId = myState.Hand[handIndex].CardId;
            if (!CardConfigManager.Instance.TryGet(cardId, out var cfg))
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "卡牌配置不存在"); return; }

            if (cfg.Cost > myState.CurrentCost)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "费用不足"); return; }

            myState.Hand.RemoveAt(handIndex);
            myState.CurrentCost -= cfg.Cost;

            if (cfg.Type == "Spell")
            {
                ApplySpellEffect(player, cfg, req);
                var enemyId = player._id == Player1._id ? Player2._id : Player1._id;
                string spellResult = States[enemyId].Hp <= 0 ? "settled" : "None";
                BroadcastBattleState(req.ActionId, spellResult, 0);
                return;
            }

            if (myState.Board.Count >= 5)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "场地已满"); return; }

            var instance = CreateCardInstance(cardId, cfg.Hp, cfg.Attack);
            instance._canAttack = false;
            myState.Board.Add(instance);

            Console.WriteLine($"{player._name} 打出卡牌 {cardId}，费用剩余 {myState.CurrentCost}");
            BroadcastBattleState(req.ActionId, "None", 0);
        }

        private CardInstance CreateCardInstance(int cardId, int hp, int atk)
        {
            return new CardInstance(_cardInstanceCounter++, cardId, hp, atk);
        }

        private void ApplySpellEffect(PlayerBaseData player, CardConfigData cfg, UseCardPackage req)
        {
            var myState = States[player._id];
            switch (cfg.EffectType)
            {
                case "DrawCard":
                    DrawCard(myState, cfg.EffectValue);
                    Console.WriteLine($"{player._name} 使用「抽取」抽了 {cfg.EffectValue} 张牌");
                    break;
                case "RDAndDrawCard":
                    var enemyData = player._id == Player1._id ? Player2 : Player1;
                    var enemyState = States[enemyData._id];

                    var rand = new Random();
                    int totalTargets = enemyState.Board.Count + 1; // 随从 + 英雄
                    int pick = rand.Next(totalTargets);
                    if (pick < enemyState.Board.Count)
                    {
                        var target = enemyState.Board[pick];
                        target._currentHealth -= cfg.EffectValue;
                        Console.WriteLine($"{player._name} 随机伤害命中随从 {target._cardId}，造成 {cfg.EffectValue} 点伤害");
                        enemyState.Board.RemoveAll(c => c._currentHealth <= 0);
                    }
                    else
                    {
                        enemyState.Hp -= cfg.EffectValue;
                        Console.WriteLine($"{player._name} 随机伤害命中 {enemyData._name} 英雄，造成 {cfg.EffectValue} 点伤害");
                    }
                    DrawCard(myState, cfg.EffectValue);
                    Console.WriteLine($"{player._name} 使用「随机伤害」造成了 {cfg.EffectValue} 点伤害，同时抽了 {cfg.EffectValue} 张牌");
                    break;
                case "AddAllAttack":
                    foreach (var card in myState.Board)
                    {
                        card._currentAttack += cfg.EffectValue;
                    }
                    Console.WriteLine($"{player._name} 使用「全体攻击」使所有随从攻击力增加 {cfg.EffectValue}");
                    break;
                case "HealthRecovery":
                    myState.Hp = Math.Min(myState.Hp + cfg.EffectValue, 25);
                    Console.WriteLine($"{player._name} 使用「治疗」恢复了 {cfg.EffectValue} 点生命，当前 HP: {myState.Hp}");
                    break;
            }
        }

        public void HandleAttack(PlayerBaseData player, AttackPackage req)
        {
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];

            if (CurrentTurnPlayer._id != player._id)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "还没到你的回合"); return; }

            var attacker = myState.Board.FirstOrDefault(c => c._instanceId == req.AttackerInstanceId);
            if (attacker == null)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "攻击者不存在"); return; }

            if (!attacker._canAttack || attacker._attackUsed > 0)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "该随从本回合无法攻击"); return; }

            int damageDealt = 0;
            string gameResult = "None";
            int attackerInstanceId = attacker._instanceId;
            int targetInstanceId = req.TargetInstanceId;

            if (req.TargetInstanceId == -1)
            {
                if (enemyState.Board.Count > 0)
                { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "敌方有随从，不能直攻英雄"); return; }

                damageDealt = attacker._currentAttack;
                enemyState.Hp -= damageDealt;
                attacker._attackUsed++;
                attacker._canAttack = false;

                Console.WriteLine($"{player._name} 直攻英雄造成 {damageDealt} 伤害，敌方 HP: {enemyState.Hp}");
                if (enemyState.Hp <= 0) gameResult = "settled";
            }
            else
            {
                var target = enemyState.Board.FirstOrDefault(c => c._instanceId == req.TargetInstanceId);
                if (target == null)
                { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "目标随从不存在"); return; }

                damageDealt = attacker._currentAttack;
                target._currentHealth -= damageDealt;
                attacker._currentHealth -= target._currentAttack;
                attacker._attackUsed++;
                attacker._canAttack = false;

                if (target._currentHealth < 0)
                {
                    int overflow = -target._currentHealth;
                    enemyState.Hp -= overflow;
                    Console.WriteLine($"{player._name} 随从攻击造成 {damageDealt} 伤害，溢出 {overflow} 打英雄，敌方 HP: {enemyState.Hp}");
                }
                else
                {
                    Console.WriteLine($"{player._name} 随从攻击造成 {damageDealt} 伤害");
                }

                enemyState.Board.RemoveAll(c => c._currentHealth <= 0);
                myState.Board.RemoveAll(c => c._currentHealth <= 0);

                if (enemyState.Hp <= 0) gameResult = "settled";
            }

            BroadcastBattleState(req.ActionId, gameResult, damageDealt,
                attackerInstanceId, targetInstanceId);
        }

        public void HandleSurrender(PlayerBaseData player)
        {
            Console.WriteLine($"{player._name} 投降");
            States[player._id].Hp = 0;
            BroadcastBattleState("surrender", "settled", 0);
        }

        public void HandleEndTurn(PlayerBaseData player, EndTurnPackage req)
        {
            if (CurrentTurnPlayer._id != player._id)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "还没到你的回合"); return; }

            CurrentTurnPlayer = CurrentTurnPlayer._id == Player1._id ? Player2 : Player1;
            var nextState = States[CurrentTurnPlayer._id];

            nextState.MaxCost = Math.Min(nextState.MaxCost + 1, 10);
            nextState.CurrentCost = nextState.MaxCost;

            foreach (var card in nextState.Board)
            {
                card._attackUsed = 0;
                card._canAttack = true;
            }

            DrawCard(nextState, 1);
            if (!IsFinished)  // 新增：抽死牌时 DrawCard 内部已广播，不要再广播一次
            {
                Console.WriteLine($"回合切换，现在轮到 {CurrentTurnPlayer._name}");
                BroadcastBattleState(req.ActionId, "None", 0);
            }
        }

        private void BroadcastBattleState(string actionId, string gameResult,
            int damageDealt, int attackerInstanceId = -1, int targetInstanceId = -1)
        {
            bool settled = gameResult == "settled";
            if (settled) IsFinished = true;
            Console.WriteLine($"===== BroadcastBattleState =====");
            Console.WriteLine($"ActionId: {actionId} | GameResult: {gameResult} | DamageDealt: {damageDealt} | Settled: {settled}");
            Console.WriteLine($"[{Player1._name}] HP: {States[Player1._id].Hp} | Cost: {States[Player1._id].CurrentCost}/{States[Player1._id].MaxCost} | Hand: {States[Player1._id].Hand.Count} | Board: {States[Player1._id].Board.Count}");
            Console.WriteLine($"[{Player2._name}] HP: {States[Player2._id].Hp} | Cost: {States[Player2._id].CurrentCost}/{States[Player2._id].MaxCost} | Hand: {States[Player2._id].Hand.Count} | Board: {States[Player2._id].Board.Count}");
            Console.WriteLine($"CurrentTurn: {CurrentTurnPlayer._name}");
            Console.WriteLine($"================================");

            SendBattleState(Player1, actionId,
                myWin: settled && States[Player1._id].Hp > 0,
                damageDealt, attackerInstanceId, targetInstanceId);

            SendBattleState(Player2, actionId,
                myWin: settled && States[Player2._id].Hp > 0,
                damageDealt, attackerInstanceId, targetInstanceId);

            if (settled) CardLogic.instance.CloseRoomByPlayer(Player1._id);
        }

        private void SendBattleState(PlayerBaseData player, string actionId, bool myWin,
            int damageDealt, int attackerInstanceId = -1, int targetInstanceId = -1)
        {
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];

            string result = "None";
            if (myWin) result = "Win";
            else if (IsFinished) result = "Lose";

            var state = new BattleState
            {
                IsMyTurn = CurrentTurnPlayer._id == player._id,
                GameResult = result,
                MyHP = myState.Hp,
                EnemyHP = enemyState.Hp,
                MyCost = myState.CurrentCost,
                MyMaxCost = myState.MaxCost,
                EnemyCost = enemyState.CurrentCost,
                EnemyMaxCost = enemyState.MaxCost,
                EnemyHandCount = enemyState.Hand.Count,
                MyDeckCount = myState.Deck.Count,
                EnemyDeckCount = enemyState.Deck.Count,
                MyHand = myState.Hand.Select(h => new HandCardState
                {
                    CardId = h.CardId,
                    InstanceId = h.InstanceId
                }).ToList(),

                MyField = myState.Board.Select(c => new MiniCardState
                {
                    InstanceId = c._instanceId,
                    CardId = c._cardId,
                    HP = c._currentHealth,
                    Attack = c._currentAttack,
                    AttackUsed = c._attackUsed,
                    CanAttack = c._canAttack
                }).ToList(),

                EnemyField = enemyState.Board.Select(c => new MiniCardState
                {
                    InstanceId = c._instanceId,
                    CardId = c._cardId,
                    HP = c._currentHealth,
                    Attack = c._currentAttack,
                    AttackUsed = 0,
                    CanAttack = false
                }).ToList(),

                LastAttackerInstanceId = attackerInstanceId,
                LastTargetInstanceId = targetInstanceId
            };

            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(state));
            using var netPkg = new NetPackage();
            netPkg.WriteInt((int)SessionMessageID.BattleStateSync);
            netPkg.WriteInt(data.Length);
            netPkg.WriteBytes(data);
            CardLogic.instance.SendToPlayer(player, netPkg);
        }

        private void SendAck(PlayerBaseData player, string actionId, ErrorCode code, string reason)
        {
            var ack = new ActionAck { ActionId = actionId, ErrorCode = code, Reason = reason };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ack));
            using var netPkg = new NetPackage();
            netPkg.WriteInt((int)SessionMessageID.BattleActionAck);
            netPkg.WriteInt(data.Length);
            netPkg.WriteBytes(data);
            CardLogic.instance.SendToPlayer(player, netPkg);
        }

        public void WhoIsFirst()
        {
            var rand = new Random();
            CurrentTurnPlayer = (rand.Next(0, 2) == 0) ? Player1 : Player2;
        }

        public void DrawCard(PlayerBattleState state, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (state.Deck.Count > 0)
                {
                    if (state.Hand.Count >= 7)
                    {
                        state.Deck.RemoveAt(0);
                        Console.WriteLine("手牌已满7张，爆牌丢弃");
                        continue;
                    }
                    int cardId = state.Deck[0];
                    state.Deck.RemoveAt(0);
                    var handCard = new HandCard
                    {
                        CardId = cardId,
                        InstanceId = _handCardCounter++
                    };
                    state.Hand.Add(handCard);
                }
                else
                {
                    state.Hp = 0;
                    BroadcastBattleState("draw_card", "settled", 0);
                    break;
                }
            }
        }
    }
}
