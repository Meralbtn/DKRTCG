using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = System.Random;
using UnityEngine.UI;
using TMPro;
using System.Data;
using CardGameClient;
using UnityEngine.SceneManagement;

namespace CardGame
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance;
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #region variable
        //prefab UI部分
        public GameObject _playerHands;
        public GameObject _enemyHands;
        public MiniCardGridManager _playerCardPlace;
        public MiniCardGridManager _enemyCardPlace;
        public HealthBar _PlayerCostUI;
        public HealthBar _EnemyCostUI;
        public HealthBar _PlayerHealthUI;
        public HealthBar _EnemyHealthUI;
        public GameObject _battleCardPrefab;
        //头像
        public GameObject playerIcon;
        public GameObject enemyIcon;

        //gameRule
        public int _maxHandCardNum = 10;
        public int _maxHealthPoint = 25;
        public int _maxCostPoint = 10;

        //playersData
        public BattleInfo _playerIsFirst;
        public BattleInfo _enemyIsFirst;
        public List<Card> _playerDeck = new List<Card>();
        public List<Card> _enemyDeck = new List<Card>();
        public List<Card> _playerDrawCards = new List<Card>();
        public List<Card> _enemyDrawCards = new List<Card>();
        public List<Card> _playerMiniCards = new List<Card>();
        public List<Card> _enemyMiniCards = new List<Card>();
        public int _playerMaxHealthPoint = 25;
        public int _enemyMaxHealthPoint = 25;
        public int _playerHealthPoint = 25;
        public int _enemyHealthPoint = 25;
        public int _playerCostPoint = 0;
        public int _enemyCostPoint = 0;
        public int _playerTrueCostPoint = 0;
        public int _enemyTrueCostPoint = 0;
        public bool _isMyTurn = false;
        public GameState _gameState = GameState.GameStart;
        #endregion

        public Action<string> ChangeBattleState;

        private void OnStateChange()
        {
            ChangeBattleState?.Invoke(_gameState.ToString());
        }
        private void Start()
        {
            CardClient.Instance.OnBattleStateReceived += ApplyServerState;
            CardClient.Instance.OnActionAckReceived += OnActionAck;
        }

        //确保当场景销毁时取消事件订阅，避免潜在的内存泄漏或空引用错误
        private void OnDestroy()
        {
            if (CardClient.Instance == null) return;
            CardClient.Instance.OnBattleStateReceived -= ApplyServerState;
            CardClient.Instance.OnActionAckReceived -= OnActionAck;
        }


        //开始游戏，加载双方玩家的数据,加载UI
        //卡组洗牌，抽牌
        //回合结束,游戏阶段
        public void GameStart(BattleStartPackage initInfo)
        {
            //读取双方数据
            Debug.Log($"GameStart 收到数据");
            Debug.Log($"MyHand 数量: {initInfo.MyHand?.Count ?? 0}");
            Debug.Log($"EnemyHand 数量: {initInfo.EnemyHand}");
            Debug.Log($"YourRole: {initInfo.YourRole}");
            FirstPlayerGenerate(initInfo.YourRole);
            _playerMaxHealthPoint = initInfo.MyMaxHealth;
            _playerHealthPoint = initInfo.MyMaxHealth;
            _enemyMaxHealthPoint = initInfo.EnemyMaxHealth;
            _enemyHealthPoint = initInfo.EnemyMaxHealth;
            _isMyTurn = initInfo.YourRole == "First";
            _playerCostPoint = initInfo.MyInitialCost;
            _playerTrueCostPoint = initInfo.MyInitialCost;
            _enemyCostPoint = initInfo.EnemyInitialCost;
            _enemyTrueCostPoint = initInfo.EnemyInitialCost;

            InitHandsFromServer(initInfo.MyHand, initInfo.EnemyHand);
            _gameState = _isMyTurn ? GameState.PlayerTurn : GameState.EnemyTurn;
            UpdateBaseUI();
            // 延迟一帧广播，确保所有监听者都已注册
            StartCoroutine(BroadcastInitialState());
        }



        private IEnumerator BroadcastInitialState()
        {
            yield return null;  // 等一帧
            ChangeBattleState?.Invoke(_gameState.ToString());
            OnStateChange();
        }


        //先后手判定
        private void FirstPlayerGenerate(string yourRole)
        {
            if (yourRole == "First")
            {
                _playerIsFirst = BattleInfo.First;
                _enemyIsFirst = BattleInfo.Last;
            }
            else
            {
                _playerIsFirst = BattleInfo.Last;
                _enemyIsFirst = BattleInfo.First;
            }

            // 通知手牌 UI 脚本
            _playerHands.GetComponent<PlayerHandUI>()._playerId = _playerIsFirst;
            _enemyHands.GetComponent<PlayerHandUI>()._playerId = _enemyIsFirst;

            // 设置场地的归属
            _playerCardPlace._user = _playerIsFirst;
            _enemyCardPlace._user = _enemyIsFirst;
            Debug.Log("[BattleManager] Waiting for initial server state...");
        }

        private void InitHandsFromServer(List<HandCardState> myHand, int enemyHandCount)
        {
            Debug.Log($"Initializing hands from server data: MyHandCount={myHand?.Count ?? 0}, EnemyHandCount={enemyHandCount}");
            //清空本地可能存在的旧数据
            _playerDrawCards.Clear();
            _enemyDrawCards.Clear();

            _playerHands.GetComponent<PlayerHandUI>().SyncFromServer(myHand);
            _enemyHands.GetComponent<PlayerHandUI>().SyncEnemyFromServer(enemyHandCount);
        }



        public void OnClickTurnEnd()
        {

            if (!_isMyTurn) return;
            Debug.Log("End Turn clicked");
            _ = CardClient.Instance.SendEndTurn();
        }

        public void RequestPlayCard(int cardInstanceId)
        {
            if (!_isMyTurn) return;
            _ = CardClient.Instance.SendPlayCard(cardInstanceId);
        }

        public void RequestAttack(int attackerInstanceId, int targetInstanceId)
        {
            if (!_isMyTurn) return;
            _ = CardClient.Instance.SendAttack(attackerInstanceId, targetInstanceId);
        }

        private void ApplyServerState(BattleState state)
        {
            Debug.Log($"Received BattleState update from server: MyHP={state.MyHP}, EnemyHP={state.EnemyHP}, MyCost={state.MyCost}, EnemyCost={state.EnemyCost}, IsMyTurn={state.IsMyTurn}");
            _playerHealthPoint = state.MyHP;
            _enemyHealthPoint = state.EnemyHP;
            _playerTrueCostPoint = state.MyCost;
            _enemyTrueCostPoint = state.EnemyCost;
            _playerCostPoint = state.MyMaxCost;
            _enemyCostPoint = state.EnemyMaxCost;
            _isMyTurn = state.IsMyTurn;
            _gameState = state.IsMyTurn ? GameState.PlayerTurn : GameState.EnemyTurn;
            SyncHand(state.MyHand, state.EnemyHandCount);
            SyncField(_playerCardPlace, state.MyField);
            SyncField(_enemyCardPlace, state.EnemyField);

            ChangeBattleState?.Invoke(_gameState.ToString());
            UpdateBaseUI();

            if (state.GameResult != "None") WinCheck(state.GameResult);
        }
        private void SyncHand(List<HandCardState> myHand, int enemyHandCount)
        {
            _playerHands.GetComponent<PlayerHandUI>().SyncFromServer(myHand);
            _enemyHands.GetComponent<PlayerHandUI>().SyncEnemyFromServer(enemyHandCount);
        }






        private void SyncField(MiniCardGridManager grid, List<MiniCardState> serverCards)
        {
            if (serverCards == null) return;
            bool isEnemy = grid == _enemyCardPlace;
            grid.SyncFromServer(serverCards, isEnemy);
        }


        // WinCheck 改为接收服务器结果，不自己判断
        private void WinCheck(string result)
        {
            if (result == "Win")
            {
                EventCenter.Broadcast(UIEvent.ON_BATTLE_WIN);
                StartCoroutine(ReturnToMainMenu());
            }
            if (result == "Lose")
            {
                EventCenter.Broadcast(UIEvent.ON_BATTLE_LOSE);
                StartCoroutine(ReturnToMainMenu());
            }
        }

        private IEnumerator ReturnToMainMenu()
        {
            yield return new WaitForSeconds(5f);
            SceneManager.LoadScene("MainMenu");
        }

        private void OnActionAck(ActionAck ack)
        {
            if (ack.ErrorCode != ErrorCode.Success)
                Debug.LogWarning($"操作被拒绝: {ack.Reason}");
        }
        public void WinCheck()
        {
            if (_playerHealthPoint <= 0)
            {
                Debug.Log("you lose");
            }
            else if (_enemyHealthPoint <= 0)
            {
                Debug.Log("you win");
            }
        }


        #region UI更新
        private void UpdateBaseUI()
        {
            _PlayerHealthUI.SetHP(_playerHealthPoint, _playerMaxHealthPoint);
            _EnemyHealthUI.SetHP(_enemyHealthPoint, _enemyMaxHealthPoint);
            _PlayerCostUI.SetHP(_playerTrueCostPoint, _playerCostPoint);
            _EnemyCostUI.SetHP(_enemyTrueCostPoint, _enemyCostPoint);
            if (_isMyTurn)
                _playerHands.GetComponent<PlayerHandUI>().RefreshOutlineByAffordability(_playerTrueCostPoint);
            else
                _playerHands.GetComponent<PlayerHandUI>().RefreshOutlineByAffordability(-1);
            RefreshMinionBorders();
            OnStateChange();
        }

        private void RefreshMinionBorders()
        {
            foreach (var inst in _playerCardPlace.GetActiveCards())
                inst.RefreshAttackVisual(inst.GetAttackUsed());
        }

        #endregion

    }
}