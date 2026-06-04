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
                if (!IsFinished)  // 防止 DrawCard 内部已广播结局
                {
                    var enemyId = player._id == Player1._id ? Player2._id : Player1._id;
                    string spellResult = States[enemyId].Hp <= 0 ? "settled" : "None";
                    BroadcastBattleState(req.ActionId, spellResult, 0);
                }
                return;
            }


            if (myState.Board.Count >= 5)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "场地已满"); return; }

            var instance = CreateCardInstance(cardId, cfg.Hp, cfg.Attack);
            instance._canAttack = false;
            myState.Board.Add(instance);
            MinionSummonEffect(player, cfg, instance);
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
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];
            var rand = new Random();
            switch (cfg.EffectType)
            {
                case "DrawCard":
                    DrawCard(myState, cfg.EffectValue);
                    Console.WriteLine($"{player._name} 使用「抽取」抽了 {cfg.EffectValue} 张牌");
                    break;
                case "RDAndDrawCard":
                    int totalTargets = enemyState.Board.Count + 1; // 随从 + 英雄
                    int pick = rand.Next(totalTargets);
                    if (pick < enemyState.Board.Count)
                    {
                        var target = enemyState.Board[pick];
                        target._currentHealth -= cfg.EffectValue;
                        Console.WriteLine($"{player._name} 随机伤害命中随从 {target._cardId}，造成 {cfg.EffectValue} 点伤害");
                        MinionDieEffect(player); // 检查死亡及其效果
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
                case "AllTKAndBackCost":
                    // 对敌方场上随机3个对象造成伤害
                    var targets = new List<CardInstance>();
                    // 收集所有可能的目标（随从 + 英雄）
                    targets.AddRange(enemyState.Board);

                    // 随机选择最多3个目标
                    int damageCount = Math.Min(3, targets.Count + 1); // +1 是为了考虑英雄
                    var selectedTargets = new HashSet<int>();
                    for (int i = 0; i < damageCount; i++)
                    {
                        int randomIndex = rand.Next(enemyState.Board.Count + 1);

                        if (randomIndex < enemyState.Board.Count)
                        {
                            // 对随从造成伤害
                            var target = enemyState.Board[randomIndex];
                            target._currentHealth -= 4; // 4点伤害
                            Console.WriteLine($"[禁忌书籍] 对敌方随从 {target._cardId} 造成 4 点伤害");
                        }
                        else
                        {
                            // 对英雄造成伤害
                            enemyState.Hp -= 4;
                            Console.WriteLine($"[禁忌书籍] 对敌方英雄造成 4 点伤害");
                        }
                    }
                    // 检查死亡效果
                    MinionDieEffect(player);
                    // 恢复2点费用
                    myState.CurrentCost = Math.Min(myState.CurrentCost + 2, myState.MaxCost);
                    Console.WriteLine($"{player._name} 使用「禁忌书籍」恢复了 2 点费用，当前费用: {myState.CurrentCost}/{myState.MaxCost}");
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

            // ✨ 攻击时效果判定（攻击者的效果）
            var attackerBattleResult = ApplyAttackerBattleEffect(player, attacker, req);
            

            int damageDealt = 0;
            string gameResult = "None";
            int attackerInstanceId = attacker._instanceId;
            int targetInstanceId = req.TargetInstanceId;

            if (req.TargetInstanceId == -1)
            {
                // 直接攻击英雄
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
                // 随从对随从
                var target = enemyState.Board.FirstOrDefault(c => c._instanceId == req.TargetInstanceId);
                if (target == null)
                { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "目标随从不存在"); return; }

                // ✨ 被攻击时效果判定（防守者的效果）
                var defenderBattleResult = ApplyDefenderBattleEffect(player, attacker, target);
                if (defenderBattleResult == "attack_blocked") return;
                if (defenderBattleResult == "attacker_dead")
                {
                    attacker._currentHealth = 0;
                    MinionDieEffect(player);
                    BroadcastBattleState(req.ActionId, "None", 0);
                    return;
                }
                if (attackerBattleResult == "instant_win")
            {
                target._currentHealth = 0;
                attacker._currentHealth -= target._currentAttack;
                attacker._attackUsed++;
                attacker._canAttack = false;
                return;
            }
                // 正常伤害结算
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

                MinionDieEffect(player);
                if (enemyState.Hp <= 0) gameResult = "settled";
            }

            BroadcastBattleState(req.ActionId, gameResult, damageDealt,
                attackerInstanceId, targetInstanceId);
        }

        // ✨ 攻击者的效果（攻击时触发）
        private string ApplyAttackerBattleEffect(PlayerBaseData player, CardInstance attacker, AttackPackage req)
        {
            if (!CardConfigManager.Instance.TryGet(attacker._cardId, out var cfg))
                return "normal";

            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];

            switch (cfg.EffectType)
            {
                case "BattleInstaDeath":
                    // 攻击时一击必杀对手英雄
                    Console.WriteLine($"[攻击效果] {cfg.Name} 一击必杀敌方英雄");
                    return "instant_win";

                case "BloodGet":
                    // 攻击时恢复生命
                    myState.Hp = Math.Min(myState.Hp + cfg.EffectValue, 25);
                    Console.WriteLine($"[攻击效果] {cfg.Name} 攻击时恢复 {cfg.EffectValue} 点生命，当前 HP: {myState.Hp}");
                    break;
            }

            return "normal";
        }

        // ✨ 防守者的效果（被攻击时触发）
        private string ApplyDefenderBattleEffect(PlayerBaseData player, CardInstance attacker, CardInstance target)
        {
            if (!CardConfigManager.Instance.TryGet(target._cardId, out var cfg))
                return "normal";

            switch (cfg.EffectType)
            {
                case "OnAttackedBlock":
                    // 被攻击时阻止本次攻击
                    Console.WriteLine($"[被攻击效果] {cfg.Name} 阻挡了本次攻击");
                    return "attack_blocked";

                case "OnAttackedCounter":
                    // 被攻击时反伤
                    attacker._currentHealth -= cfg.EffectValue;
                    Console.WriteLine($"[被攻击效果] {cfg.Name} 反伤 {cfg.EffectValue} 点");
                    break;
            }

            return "normal";
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

        public void HandleUseCardWithTarget(PlayerBaseData player, UseCardWithTargetPackage req)
        {
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];

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


            if (cfg.Type == "Spell")
            {
                myState.CurrentCost -= cfg.Cost;
                ApplySpellEffectWithTarget(player, cfg, req.TargetInstanceId);
                if (!IsFinished)
                {
                    string spellResult = enemyState.Hp <= 0 ? "settled" : "None";
                    BroadcastBattleState(req.ActionId, spellResult, 0);
                }
                return;
            }

            // 随从逻辑保持不变...
            if (myState.Board.Count >= 5)
            { SendAck(player, req.ActionId, ErrorCode.InvalidRequest, "场地已满"); return; }
            myState.CurrentCost -= cfg.Cost;
            var instance = CreateCardInstance(cardId, cfg.Hp, cfg.Attack);
            instance._canAttack = false;
            ApplyMinionEffectWithTarget(player, cfg, req.TargetInstanceId);
            myState.Board.Add(instance);

            BroadcastBattleState(req.ActionId, "None", 0);
        }
        private void ApplyMinionEffectWithTarget(PlayerBaseData player, CardConfigData cfg, int targetInstanceId)
        {
            // 这里可以根据 cfg.EffectType 来决定如何处理带目标的随从效果
            // 例如，如果有一个效果是 "SummonAndDamage"，它会在召唤随从的同时对一个目标造成伤害
            // 这只是一个示例，具体实现取决于你的卡牌设计
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];
            switch (cfg.EffectType)
            {
                case "TargetDie":
                    if (targetInstanceId != -1)
                    {
                        var target = enemyState.Board.FirstOrDefault(c => c._instanceId == targetInstanceId);
                        if (target != null)
                        {
                            target._currentHealth = 0;
                            Console.WriteLine($"{player._name} 召唤随从并对 {target._cardId} 造成 {cfg.EffectValue} 点伤害");
                            MinionDieEffect(player); // 检查死亡及其效果
                        }
                    }
                    break;
                // ... 其他带目标的随从效果
                case "TargetDamage":
                    if (targetInstanceId == -1)
                    {
                        enemyState.Hp -= cfg.EffectValue;
                        Console.WriteLine($"{player._name} 对敌方英雄造成 {cfg.EffectValue} 点伤害");
                    }
                    else
                    {
                        var target = enemyState.Board.FirstOrDefault(c => c._instanceId == targetInstanceId);
                        if (target != null)
                        {
                            target._currentHealth -= cfg.EffectValue;
                            Console.WriteLine($"{player._name} 对 {target._cardId} 造成 {cfg.EffectValue} 点伤害");
                            MinionDieEffect(player); // 检查死亡及其效果
                        }
                    }
                    break;
            }
        }

        private void ApplySpellEffectWithTarget(PlayerBaseData player, CardConfigData cfg, int targetInstanceId)
        {
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];

            switch (cfg.EffectType)
            {
                case "TargetDamage":  // 单体伤害
                    if (targetInstanceId == -1)
                    {
                        enemyState.Hp -= cfg.EffectValue;
                        Console.WriteLine($"{player._name} 对敌方英雄造成 {cfg.EffectValue} 点伤害");
                    }
                    else
                    {
                        var target = enemyState.Board.FirstOrDefault(c => c._instanceId == targetInstanceId);
                        if (target != null)
                        {
                            target._currentHealth -= cfg.EffectValue;
                            Console.WriteLine($"{player._name} 对 {target._cardId} 造成 {cfg.EffectValue} 点伤害");
                            MinionDieEffect(player); // 检查死亡及其效果
                        }
                    }
                    break;
                // ... 其他效果类型
                case "StealEntity":
                    if (targetInstanceId == -1)
                    {
                        return;
                    }

                    var stealTarget = enemyState.Board.FirstOrDefault(c => c._instanceId == targetInstanceId);
                    if (stealTarget == null)
                    {
                        return;
                    }
                    if (myState.Board.Count >= 5)
                    {
                        enemyState.Board.Remove(stealTarget);
                        return;
                    }
                    enemyState.Board.Remove(stealTarget);
                    stealTarget._canAttack = false;
                    myState.Board.Add(stealTarget);
                    Console.WriteLine($"{player._name} 使用「篡权」窃取了敌方随从 {stealTarget._cardId}");
                    break;

            }
        }


        private void MinionDieEffect(PlayerBaseData player)
        {
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];

            // 处理敌方死亡卡牌效果
            ProcessDieEffects(enemyState, myState, player);

            // 处理己方死亡卡牌效果
            ProcessDieEffects(myState, enemyState, player);

            // 清理所有死亡卡牌
            enemyState.Board.RemoveAll(c => c._currentHealth <= 0);
            myState.Board.RemoveAll(c => c._currentHealth <= 0);
        }

        private void ProcessDieEffects(PlayerBattleState deadState, PlayerBattleState targetState, PlayerBaseData player)
        {
            var deadCards = deadState.Board.Where(c => c._currentHealth <= 0).ToList();

            foreach (var dead in deadCards)
            {
                if (!CardConfigManager.Instance.TryGet(dead._cardId, out var dcfg))
                    continue;

                switch (dcfg.EffectType)
                {
                    case "DieSummon":
                        HandleDieSummon(deadState, dcfg);
                        break;

                    case "DieDamage":
                        HandleDieDamage(targetState, deadState, dcfg, player);
                        break;

                    case "DieDrawID":
                        HandleDieDrawID(deadState, dcfg);
                        break;
                }
            }
        }

        private void HandleDieSummon(PlayerBattleState state, CardConfigData cfg)
        {
            if (cfg.EffectValue <= 0 || !CardConfigManager.Instance.TryGet(cfg.EffectValue, out var summon))
                return;

            // 移除死亡卡牌后再召唤
            state.Board.RemoveAll(c => c._currentHealth <= 0);

            if (state.Board.Count >= 5)
            {
                Console.WriteLine($"[死亡效果] {cfg.Name} 场地已满，无法召唤 {summon.Name}");
                return;
            }

            var newCard = CreateCardInstance(summon.Id, summon.Hp, summon.Attack);
            newCard._canAttack = false;
            state.Board.Add(newCard);
            Console.WriteLine($"[死亡效果] {cfg.Name} 召唤了 {summon.Name}");
        }

        private void HandleDieDamage(PlayerBattleState targetState, PlayerBattleState sourceState, CardConfigData cfg, PlayerBaseData player)
        {
            if (cfg.EffectValue <= 0)
                return;

            var rand = new Random();
            int totalTargets = targetState.Board.Count + 1; // 随从 + 英雄
            int pick = rand.Next(totalTargets);

            if (pick < targetState.Board.Count)
            {
                var target = targetState.Board[pick];
                target._currentHealth -= cfg.EffectValue;
                Console.WriteLine($"[死亡效果] {cfg.Name} 造成 {cfg.EffectValue} 点伤害，命中随从 {target._cardId}");
                MinionDieEffect(player); // 检查连锁死亡效果
            }
            else
            {
                targetState.Hp -= cfg.EffectValue;
                var playerName = (targetState == States[Player1._id]) ? Player1._name : Player2._name;
                Console.WriteLine($"[死亡效果] {cfg.Name} 造成 {cfg.EffectValue} 点伤害，命中 {playerName} 英雄，当前 HP: {targetState.Hp}");
            }
        }

        private void HandleDieDrawID(PlayerBattleState state, CardConfigData cfg)
        {
            if (cfg.EffectValue <= 0 || !CardConfigManager.Instance.TryGet(cfg.EffectValue, out var drawCfg))
                return;

            if (state.Hand.Count >= 7)
            {
                Console.WriteLine($"[死亡效果] {cfg.Name} 手牌已满7张，无法抽取 {drawCfg.Name}");
                return;
            }

            var handCard = new HandCard
            {
                CardId = drawCfg.Id,
                InstanceId = _handCardCounter++
            };
            state.Hand.Add(handCard);
            Console.WriteLine($"[死亡效果] {cfg.Name} 抽取了 {drawCfg.Name}");
        }

        private void MinionSummonEffect(PlayerBaseData player, CardConfigData cfg, CardInstance summonedCard = null)
        {
            var myState = States[player._id];
            var enemyData = player._id == Player1._id ? Player2 : Player1;
            var enemyState = States[enemyData._id];

            switch (cfg.EffectType)
            {
                case "HealthRecovery":  // 召唤时恢复生命
                    myState.Hp = Math.Min(myState.Hp + cfg.EffectValue, 25);
                    Console.WriteLine($"[召唤效果] {cfg.Name} 恢复了玩家 {cfg.EffectValue} 点生命，当前 HP: {myState.Hp}");
                    break;

                case "OpenDamage":  // 召唤时对敌方随机目标造成伤害
                    if (enemyState.Board.Count > 0)
                    {
                        var rand = new Random();
                        var target = enemyState.Board[rand.Next(enemyState.Board.Count)];
                        target._currentHealth -= cfg.EffectValue;
                        Console.WriteLine($"[召唤效果] {cfg.Name} 对 {target._cardId} 造成 {cfg.EffectValue} 点伤害");
                        MinionDieEffect(player);  // 检查是否有死亡效果
                    }
                    break;

                case "SummonToken":  // 召唤时额外召唤衍生物
                    if (myState.Board.Count < 5)
                    {
                        if (CardConfigManager.Instance.TryGet(cfg.EffectValue, out var tokenCfg))
                        {
                            var tokenCard = CreateCardInstance(cfg.EffectValue, tokenCfg.Hp, tokenCfg.Attack);
                            tokenCard._canAttack = false;
                            myState.Board.Add(tokenCard);
                            Console.WriteLine($"[召唤效果] {cfg.Name} 召唤了 {tokenCfg.Name}");
                        }
                    }
                    break;

                case "Stronger":  // 召唤时增强场上所有随从
                    //如果费用上限为4以上，增强友军攻击力
                    if (myState.MaxCost >= 4)
                    {
                        // 只增强最后的随从
                        var lastCard = myState.Board.LastOrDefault();
                        if (lastCard != null)
                        {
                            lastCard._currentHealth += cfg.EffectValue;
                        }
                        Console.WriteLine($"[召唤效果] {cfg.Name} 增强了友军 {cfg.EffectValue} 点生命值");
                    }
                    break;
                case "DrawID":
                    // 处理抽卡效果
                    // 这里的逻辑可能需要根据具体的卡牌设计来实现，例如抽特定类型的卡牌等
                    //加入指定ID卡牌
                    if (CardConfigManager.Instance.TryGet(cfg.EffectValue, out var drawCfg))
                    {
                        if (myState.Hand.Count >= 7)
                        {
                            Console.WriteLine("手牌已满7张，无法抽取指定卡牌");
                            return;
                        }
                        var handCard = new HandCard
                        {
                            CardId = drawCfg.Id,
                            InstanceId = _handCardCounter++
                        };
                        myState.Hand.Add(handCard);
                        Console.WriteLine($"[召唤效果] {cfg.Name} 抽取了指定卡牌 {drawCfg.Name}");
                    }
                    break;
                case "SummonDouble":
                    // 召唤时2次指定目标
                    for (int i = 0; i < 2; i++)
                    {
                        if (myState.Board.Count >= 5)
                        {
                            Console.WriteLine("场地已满，无法召唤更多随从");
                            break;
                        }
                        //从配置文件取出卡牌
                        CardConfigManager.Instance.TryGet(cfg.EffectValue, out var summonCfg);
                        var instance = CreateCardInstance(cfg.EffectValue, summonCfg.Hp, summonCfg.Attack);
                        instance._canAttack = true;
                        myState.Board.Add(instance);
                        Console.WriteLine($"[召唤效果] {cfg.Name} 召唤了 {cfg.Name} 的一个复制");
                    }
                    break;
                case "DrawID2":
                    // 抽两张指定ID的卡牌
                    for (int i = 0; i < 2; i++)
                    {
                        if (myState.Hand.Count >= 7)
                        {
                            Console.WriteLine("手牌已满7张，无法抽取指定卡牌");
                            break;
                        }
                        if (CardConfigManager.Instance.TryGet(cfg.EffectValue, out var drawCfg2))
                        {
                            var handCard = new HandCard
                            {
                                CardId = drawCfg2.Id,
                                InstanceId = _handCardCounter++
                            };
                            myState.Hand.Add(handCard);
                            Console.WriteLine($"[召唤效果] {cfg.Name} 抽取了指定卡牌 {drawCfg2.Name}");
                        }
                    }
                    break;
                case "AoeAndDraw":
                    // 造成AOE伤害并抽一张牌
                    foreach (var card in enemyState.Board)
                    {
                        card._currentHealth -= cfg.EffectValue;
                        Console.WriteLine($"[召唤效果] {cfg.Name} 对 {card._cardId} 造成 {cfg.EffectValue} 点伤害");
                    }
                    MinionDieEffect(player);  // 检查是否有死亡效果
                    if (myState.Hand.Count >= 7)
                    {
                        Console.WriteLine("手牌已满7张，无法抽取指定卡牌");
                        return;
                    }
                    {
                        var handCard = new HandCard
                        {
                            CardId = 33,
                            InstanceId = _handCardCounter++
                        };
                        myState.Hand.Add(handCard);
                    }

                    Console.WriteLine($"[召唤效果] {cfg.Name} 造成了 AOE 伤害并抽了一张牌");
                    break;
                case "AoeDownHealth":
                    // 造成AOE伤害并降低敌方随从攻击力
                    foreach (var card in enemyState.Board)
                    {
                        card._currentHealth -= cfg.EffectValue;
                        card._currentAttack = Math.Max(0, card._currentAttack - 1); // 降低攻击力但不至于变负数
                        Console.WriteLine($"[召唤效果] {cfg.Name} 对 {card._cardId} 造成 {cfg.EffectValue} 点伤害并降低攻击力");
                    }
                    MinionDieEffect(player);  // 检查是否有死亡效果
                    break;
                case "Aoe":
                    // 造成AOE伤害
                    foreach (var card in enemyState.Board)
                    {
                        card._currentHealth -= cfg.EffectValue;
                        Console.WriteLine($"[召唤效果] {cfg.Name} 对 {card._cardId} 造成 {cfg.EffectValue} 点伤害");
                    }
                    MinionDieEffect(player);  // 检查是否有死亡效果
                    break;
                case "RandomDie2":
                    // 随机杀死2个敌方随从

                    for (int i = 0; i < 2; i++)
                    {
                        var rand = new Random();
                        if (enemyState.Board.Count == 0) break;
                        var target = enemyState.Board[rand.Next(enemyState.Board.Count)];
                        target._currentHealth = 0;
                        Console.WriteLine($"[召唤效果] {cfg.Name} 随机杀死了敌方随从 {target._cardId}");
                        MinionDieEffect(player);  // 检查是否有连锁死亡效果
                    }
                    break;
                case "WhoLo":
                    //对玩家自身造成3点伤害，强化自身6点攻击。召唤回合可攻击
                    myState.Hp -= 3;
                    {
                        var lastCard = myState.Board.LastOrDefault();
                        if (lastCard != null)
                        {
                            lastCard._currentAttack += 6;
                            lastCard._canAttack = true;
                        }
                    }
                    Console.WriteLine($"[召唤效果] {cfg.Name} 对玩家造成了 3 点伤害，强化了自身 6 点攻击，并且可以立即攻击");
                    break;
                case "SummonAttack":
                    summonedCard._canAttack = true;
                    Console.WriteLine($"[召唤效果] 可以立即攻击");
                    break;
                case "MimiKeke":
                    // 召唤米米可可
                    if (myState.Board.Count >= 5)
                    {
                        Console.WriteLine($"[召唤效果] {cfg.Name} 场地已满，无法召唤 米米可可");
                        return;
                    }
                    {
                        CardConfigManager.Instance.TryGet(23, out var mimiCfg);
                        var instance = CreateCardInstance(23, mimiCfg.Hp + 2, mimiCfg.Attack + 2);
                        instance._canAttack = true;
                        myState.Board.Add(instance);
                    }
                    if (myState.Board.Count >= 5)
                    {
                        Console.WriteLine($"[召唤效果] {cfg.Name} 场地已满，无法召唤 米米可可");
                        return;
                    }
                    {
                        CardConfigManager.Instance.TryGet(24, out var mimiCfg);
                        var instance = CreateCardInstance(24, mimiCfg.Hp + 2, mimiCfg.Attack + 2);
                        instance._canAttack = true;
                        myState.Board.Add(instance);
                    }
                    break;
            }
        }


    }
}
