using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour, I_Interactable
{
    private float focusTimeCounter = 0.0f;

    public void OnInteract()
    {
        Collected();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            Collected();
    }

    private void Collected()
    {
        focusTimeCounter += Time.deltaTime;
        if (focusTimeCounter > 0.01)
        {
            FindObjectOfType<GameManager>().AddToScore(1);
            FindObjectOfType<GameManager>().UpdateScoreText(1);
            FindObjectOfType<CoinCollectSound>().PlayCollectSound();
            print("Coin collected, name:" + gameObject.name);
            Destroy(gameObject);
            ///GetComponent<MeshRenderer>().enabled = false;
            ///Destroy(GetComponent<MeshRenderer>());
        }
        else
        {
            print("name:" + gameObject.name + ",collect counter:" + focusTimeCounter);
        }
    }
}
