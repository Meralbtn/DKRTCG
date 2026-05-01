using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CardGame
{
    public class CardDataHandle : MonoBehaviour
    {
        public TextAsset csvFile;
        private List<Card> pile;
        public GameObject cardPrefab;
        [ContextMenu("Load Cards")] // 在组件右键菜单点击即可触发
        public void  OnButtonClick() {
            // 1. 按行分割
            string[] lines = csvFile.text.Split('\n');

            // 2. 遍历每一行（跳过第一行表头）
            for (int i = 1; i < lines.Length; i++) {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                // 3. 按逗号分割每列
                string[] data = lines[i].Split(',');

                string type = data[0];
            
                // 4. 根据类型进行多态处理
                if (type == "Minion") {
                    CreateMinionCard(data);
                }
                // 以后可以在这里增加 Spell, Weapon 等
            }
        }
        public void CreateMinionCard(string[] data) {
            // 创建 ScriptableObject 实例
            MinionCard newCard = new MinionCard(int.Parse(data[1]),data[2],int.Parse(data[5]),int.Parse(data[4]),int.Parse(data[3]));
            // 数据填充
            newCard.Type = CardType.Minion;
            var w = Instantiate(cardPrefab);
            CardUIManager ui = w.GetComponent<CardUIManager>();
            ui.card = newCard;
            ui.InitialCard();
            Debug.Log($"读取成功: {newCard.CardName} (ATK:{newCard.Attack})");
        
            // 如果是在编辑器下，你可以用 AssetDatabase.CreateAsset 将其保存为 .asset 文件
        }
    }
}