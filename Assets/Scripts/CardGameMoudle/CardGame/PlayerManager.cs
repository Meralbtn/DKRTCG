//初始化玩家信息，卡组信息
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using CardGameApp;
using System.Threading.Tasks;
namespace CardGame
{
    public enum PlayerState
    {
        InRoom,
        Battle,
        Wait
    }
    public class PlayerManager
    {
        private PlayerBaseData _playerData = new PlayerBaseData();
        private bool _isLogin = false;
        //房间
        private CardRoom _cardRoom;
        public List<CardRoomInfo> _roomList = new List<CardRoomInfo>();
        //设计为单例，用来存储和读取玩家信息
        private Dictionary<string, Deck> _decks = new Dictionary<string, Deck>();
        private List<Card> _cards = new List<Card>();
        //需要修改
        private Deck _tempDeck = new Deck();
        private Deck _battleDeck = new Deck();
        private static PlayerManager _manager;

        public List<Deck> _saveDecks = new List<Deck>();
        public Deck _aiEnemyDeck = new Deck();
        private PlayerState _playerState = PlayerState.Wait;

        public string PlayerName = "Player1";

        public List<Card> GetCardList()
        {
            return _cards;
        }

        private PlayerManager() { }
        public static PlayerManager Instance()
        {
            if (_manager == null)
            {
                _manager = new PlayerManager();
                _manager.InitialSystemCard();
                _manager.GetAllSavedDecks();
            }
            return _manager;
        }

        public bool IsLogin()
        {
            return _isLogin;
        }


        private void InitialSystemCard()
        {
            TextAsset csvFile = Resources.Load<TextAsset>("CardData/卡牌信息");
            if (csvFile == null) { Debug.Log("Load System Card Error"); return; }

            string[] lines = csvFile.text.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] data = lines[i].Split(',');
                if (data.Length < 2) continue;

                string type = data[0].Trim();
                if (type == "Minion") CreateMinionCard(data);
                else if (type == "Spell") CreateSpellCard(data);
            }


            for (int i = 0; i < 10; i++)
                _battleDeck.AddCard(0);

            // 抽取法术牌 ID=29，放2张
            for (int i = 0; i < 2; i++)
                _battleDeck.AddCard(29);
        }

        public void CreateSpellCard(string[] data)
        {
            int id = int.Parse(data[1].Trim());
            string name = data[2].Trim();
            int cost = int.Parse(data[5].Trim());
            string effectType = data.Length > 6 ? data[6].Trim() : "None";
            int effectValue = data.Length > 7 && !string.IsNullOrWhiteSpace(data[7]) ? int.Parse(data[7].Trim()) : 0;
            string description = data.Length > 8 ? data[8].Trim() : "";
            bool needsTarget = effectType == "SpellDamageTarget";

            SpellCard card = new SpellCard(id, name, cost, description, effectType, effectValue, needsTarget, description);
            _cards.Add(card);
            Debug.Log($"读取法术: {card.CardName} (Cost:{card.Cost})");
        }

        public void CreateMinionCard(string[] data)
        {
            int id = int.Parse(data[1].Trim());
            string name = data[2].Trim();
            int hp = int.Parse(data[3].Trim());
            int atk = int.Parse(data[4].Trim());
            int cost = int.Parse(data[5].Trim());
            string effectType = data.Length > 6 ? data[6].Trim() : "None";
            int effectValue = data.Length > 7 && !string.IsNullOrWhiteSpace(data[7]) ? int.Parse(data[7].Trim()) : 0;
            string description = data.Length > 8 ? data[8].Trim() : "";

            MinionCard card = new MinionCard(id, name, cost, atk, hp, effectType, effectValue, description);
            _cards.Add(card);
            Debug.Log($"读取随从: {card.CardName} (ATK:{card.Attack} HP:{card.Health})");
        }

        //从文件区域读取玩家的保存信息
        //保存为json格式的文件
        //deck
        //id num 键值对
        private string GetSavePath()
        {
            string path = Path.Combine(Application.persistentDataPath, "PlayerData");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
        private string GetDeckPath(string deckName)
        {
            return Path.Combine(GetSavePath(), deckName + ".json");
        }
        public async Task SaveDeckAsync()
        {
            if (!_tempDeck.IsLegal())
            {
                Debug.LogWarning("卡组不合法，无法保存");
                return;
            }

            // 序列化在主线程做（访问 Unity 对象必须在主线程）
            string deckPath = GetDeckPath(_tempDeck._deckName);
            string json = JsonConvert.SerializeObject(_tempDeck, Formatting.Indented);

            // 文件 IO 丢到后台线程
            try
            {
                await Task.Run(() => File.WriteAllText(deckPath, json));

                // 回到主线程更新数据（await 之后自动回主线程）
                _decks[_tempDeck._deckName] = _tempDeck;
                _saveDecks.RemoveAll(d => d._deckName == _tempDeck._deckName);
                _saveDecks.Add(_tempDeck);
                Debug.Log($"卡组已保存至: {deckPath}");
            }
            catch (Exception e)
            {
                Debug.LogError("保存失败: " + e.Message);
            }
        }

        public async Task DeleteDeckAsync(Deck deck)
        {
            string deckPath = GetDeckPath(deck._deckName);
            try
            {
                if (File.Exists(deckPath))
                    await Task.Run(() => File.Delete(deckPath));

                _decks.Remove(deck._deckName);
                _saveDecks.Remove(deck);
                Debug.Log("卡组已删除: " + deck._deckName);
            }
            catch (Exception e)
            {
                Debug.LogError("删除失败: " + e.Message);
            }
        }

        private void SaveDeck(Deck deck)
        {
            //直接用json自带的序列化保存
            string savePath = Path.Combine(Application.persistentDataPath, "PlayerData");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            string deckPath = Path.Combine(savePath, deck._deckName + ".json");
            string json = JsonConvert.SerializeObject(deck, Formatting.Indented);
            File.WriteAllText(deckPath, json);
            Debug.Log("卡组已保存至:{deckPath}");
        }
        //debugtest1
        public void SaveDeck()
        {
            if (_tempDeck.IsLegal() == false)
            {
                Debug.Log("save deck no");
            }
            else
                Debug.Log("save deck yes");
            //直接用json自带的序列化保存
            string savePath = Path.Combine(Application.persistentDataPath, "PlayerData");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            string deckPath = Path.Combine(savePath, _tempDeck._deckName + ".json");
            Debug.Log($"Saving deck to: {deckPath}");
            try
            {
                string json = JsonConvert.SerializeObject(_tempDeck, Formatting.Indented);
                File.WriteAllText(deckPath, json);
                Debug.Log("卡组已保存至: " + deckPath);
                _decks.Add(_tempDeck._deckName, _tempDeck);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to save deck: " + e.Message);
            }
        }



        //加载保存的卡组数据
        public void GetAllSavedDecks()
        {
            _saveDecks.Clear();
            string savePath = Path.Combine(Application.persistentDataPath, "PlayerData");
            if (!Directory.Exists(savePath)) return;
            string[] files = Directory.GetFiles(savePath, "*.json");
            try
            {
                foreach (var value in files)
                {
                    string json = File.ReadAllText(value);
                    Deck deck = JsonConvert.DeserializeObject<Deck>(json);
                    _saveDecks.Add(deck);
                    if (deck._deckName == "AIDefaultDeck")
                    {
                        Debug.Log("AIDefaultDeck loaded");
                        _aiEnemyDeck = deck;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load decks from: " + savePath);
                Debug.LogError("Failed to save deck: " + e.Message);
                return;
            }
        }


        //对战系统前的准备
        public bool ChoseDeck(string deckName)
        {
            if (_decks.TryGetValue(deckName, out var deck) && deck.IsLegal())
            {
                _battleDeck = deck;
                return true;
            }
            return false;
        }

        public void ChoseDeck()
        {
            if (_tempDeck.IsLegal() == true)
            {
                _battleDeck = _tempDeck;
            }
            else
            {
                Debug.Log("CardISnot");
            }
        }
        //debugtest2


        //玩家读取Deck
        public void LoadPlayerDecks()
        {
            _decks.Clear();
            string savePath = Path.Combine(Application.persistentDataPath, "PlayerData");
            if (!Directory.Exists(savePath)) return;
            string[] files = Directory.GetFiles(savePath, "*.json");
            foreach (var value in files)
            {
                string json = File.ReadAllText(value);
                Deck deck = JsonConvert.DeserializeObject<Deck>(json);
                _decks.Add(deck._deckName, deck);
            }
        }

        //删除卡组数据 暂时不写
        public void AddCardToDeck(int id)
        {
            if (_tempDeck.GetCardCount() == 40)
                return;
            if (!_tempDeck._cards.TryGetValue(id, out var count))
            {
                _tempDeck.AddCard(id);
                return;
            }
            if (count < 3)
                _tempDeck.AddCard(id);
        }

        public void DeleteCardToDeck(int id)
        {
            if (!_tempDeck._cards.TryGetValue(id, out var count))
            {
                return;
            }
            _tempDeck.DeleteCard(id);
        }
        //获取卡组
        public Deck GetTempDeck()
        {
            return _tempDeck;
        }
        public Deck GetBattleDeck()
        {
            return _battleDeck;
        }

        #region 联机房间
        public void CreateRoomHost(string roomName, int roomId)
        {
            _playerState = PlayerState.InRoom;
            _cardRoom = new CardRoom(roomName, _playerData, roomId);
        }

        public void JoinRoom(List<PlayerBaseData> players, string roomName, int roomId)
        {
            _playerState = PlayerState.InRoom;
            _cardRoom = new CardRoom(roomName, players[0], roomId);
            _cardRoom.JoinRoom(players[1]);
        }



        public void LeaveRoom()
        {
            _playerState = PlayerState.Wait;
            _cardRoom.LeaveRoom(_playerData);
            _cardRoom = null;
        }

        public void UpdateRoomPlayers(List<PlayerBaseData> players)
        {
            // 更新当前房间的玩家列表
            if (_cardRoom != null)
                _cardRoom._players = players;
        }

        public CardRoom GetCardRoomInfo()
        {
            return _cardRoom;
        }

        public void SetPlayerData(string id)
        {
            _playerData._id = id;
            _playerData._name = PlayerName;
            _playerData._deck = _battleDeck.DeckToNetList();
        }
        public void SetPlayerName(string name)
        {

            _playerData._name = name;
        }

        //同步登录信息
        public void SetLoginState()
        {
            _isLogin = true;
        }
        public PlayerBaseData GetPlayerData()
        {
            return _playerData;
        }
        #endregion


        //卡组编辑
        // 新建一个空卡组并设为当前编辑目标
        public void CreateNewDeck(string deckName)
        {
            _tempDeck = new Deck();
            _tempDeck._deckName = deckName;
        }

        // 选择一个已有卡组进行编辑
        public void SelectDeckForEdit(Deck deck)
        {
            // 深拷贝，防止编辑时直接改动原数据
            string json = JsonConvert.SerializeObject(deck);
            _tempDeck = JsonConvert.DeserializeObject<Deck>(json);
        }

        // 删除卡组
        public void DeleteDeck(string deckName)
        {
            string savePath = Path.Combine(Application.persistentDataPath, "PlayerData");
            string deckPath = Path.Combine(savePath, deckName + ".json");

            if (File.Exists(deckPath))
                File.Delete(deckPath);

            _saveDecks.RemoveAll(d => d._deckName == deckName);
            _decks.Remove(deckName);
            Debug.Log($"卡组 {deckName} 已删除");
        }

        // 获取 tempDeck 的卡牌数量
        public int GetTempDeckCount()
        {
            return _tempDeck.GetCardCount();
        }

        //选择卡组
        public void SelectBattleDeck(Deck deck)
        {
            _battleDeck = deck;
            Debug.Log($"已选择卡组: {deck._deckName}");
        }
    }
}