using TMPro;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int gold = 100;
    public int bookPrice = 2000;
    public TMP_Text basicGoldText;
    public TMP_Text shopGoldText;

    void Awake()
    {
        RefreshGold();
    }

    public void Spend(int amount)
    {
        gold -= amount;
        RefreshGold();
    }

    public void RefreshGold()
    {
        string value = gold.ToString();
        if (basicGoldText != null)
            basicGoldText.text = value;
        if (shopGoldText != null)
            shopGoldText.text = value;
    }
}
