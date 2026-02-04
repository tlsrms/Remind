using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool IsGameActive { get; private set; } = true;
    
    private int capturedTowerCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartGame()
    {
        IsGameActive = true;
        capturedTowerCount = 0;
    }

    public void GameOver(bool isWin, string message)
    {
        IsGameActive = false;
        Debug.Log($"Game Over: {message}");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowResult(isWin, message);
        }
    }

    public void OnTowerCaptured()
    {
        capturedTowerCount++;
        Debug.Log($"Objective Updated: Tower Captured! ({capturedTowerCount}/2)");
        
        if (capturedTowerCount >= 2)
        {
            GameOver(true, "Domination Victory!\n(Captured 2 Towers)");
        }
    }

    public void OnHelipadReached(bool hasRadio)
    {
        if (hasRadio)
        {
            GameOver(true, "Extraction Victory!\n(Escaped with Radio)");
        }
        else
        {
            Debug.Log("Helipad reached. You need a Radio to call for extraction.");
        }
    }
}