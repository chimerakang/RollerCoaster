using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour, I_Interactable
{
    public void OnInteract()
    {
        Collected();
    }

    private void Collected()
    {
        print("Coin collected.");
    }
}
