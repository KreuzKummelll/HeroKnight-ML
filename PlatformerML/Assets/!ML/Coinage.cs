using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coinage : MonoBehaviour
{

    
    [SerializeField] int coinsPerWave = 10;
    [SerializeField] List<GameObject> coins;
    [SerializeField] List<int> activeCoinIndexes;


    private void OnEnable()
    {
        ClearAndRefreshIndexes();
    }

    void ActivateCoins()
    {
        if (activeCoinIndexes.Count < 1) return;
        for (int i = 0; i < activeCoinIndexes.Count; i++)
        {
            coins[activeCoinIndexes[i]].SetActive(true);
        }
    }

    void ClearAndRefreshIndexes()
    {
        ClearIndeces();
        GetIndexes();
    }

    void ClearIndeces()
    {
        activeCoinIndexes.Clear();
    }
    void DeactivateAllCoins()
    {
        foreach (var coin in coins)
        {
            coin.SetActive(false);
        }
    }
    void GetIndexes()
    {
        for (int i = 0; i < coinsPerWave; i++)
        {
            int randomIndex = RandomInt();
            activeCoinIndexes.Add(randomIndex);
        }
    }

    public void RefreshCoinage()
    {
        ClearAndRefreshIndexes();
        DeactivateAllCoins();
        ActivateCoins();
    }
   


    int RandomInt()
    {
        int random = UnityEngine.Random.Range(0, coins.Count);
        return random;
    }
    
}
