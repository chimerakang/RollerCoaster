using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
}
