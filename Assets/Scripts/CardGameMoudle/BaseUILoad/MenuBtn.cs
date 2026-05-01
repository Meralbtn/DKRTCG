using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardGame
{
    public class MenuBtn : MonoBehaviour
    {
        #region ChangeScene
        public void MainToDeck()
        {
            SceneManager.LoadScene("Scenes/ChoseDeck");
        }
        public void DeckToMain()
        {
            SceneManager.LoadScene("Scenes/MainMenu");
        }
        #endregion

        #region DeckScene
        public void SaveDeckClicked()
        {
            PlayerManager.Instance().SaveDeck();
        }
        #endregion
        
        
    }
}