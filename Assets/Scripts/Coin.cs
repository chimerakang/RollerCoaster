using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour, I_Interactable
{
    public void OnInteract()
    {
        Collected();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
            Collected();
    }

    private void Collected()
    {
        FindObjectOfType<GameManager>().AddToScore(1);
        FindObjectOfType<GameManager>().UpdateScoreText(1);
        print("Coin collected.");
        Destroy(this.gameObject);
    }
}
