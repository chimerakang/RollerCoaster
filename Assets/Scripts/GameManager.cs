using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GameManager : MonoBehaviour
{
    public Text scoreText;
    public int score = 0;

    public void AddToScore(int scoreToAdd)
    {
        score = score + scoreToAdd;
        print("New score is " + score);
    }

    public void UpdateScoreText(int scoreToAdd)
    {
        int totalScore = score + scoreToAdd;
        scoreText.text = totalScore.ToString();
    }

    public void TriggerEndGameRoutine()
    {
        StartCoroutine(EndGame());
    }

    private IEnumerator EndGame()
    {
        GameObject.FindGameObjectWithTag("EndGameText").GetComponent<TextMeshProUGUI>().text = "You collected " + score.ToString() + " coins!";
        yield return null;
    }
}
