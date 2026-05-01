using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class HealthBar : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI hpText;

    public void SetHP(int current, int max)
    {
        slider.value = (float)current / max;
        hpText.text = $"{current}/{max}";
    }
}