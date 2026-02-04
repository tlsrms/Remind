using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HP UI")]
    public GameObject[] hpIcons; 

    [Header("Game Result UI")]
    public GameObject gameResultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultMessageText;

    [Header("Indicators")]
    public DirectionIndicator directionIndicator;

    [Header("Status UI")]
    public TextMeshProUGUI mineAlertText;
    public TextMeshProUGUI reconInfoText; // New

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowReconInfo(string info)
    {
        if (reconInfoText != null)
        {
            reconInfoText.text = info;
            reconInfoText.gameObject.SetActive(true);
            
            // Auto-hide after a few seconds (Optional, handled by coroutine if needed)
            CancelInvoke(nameof(HideReconInfo));
            Invoke(nameof(HideReconInfo), 3f);
        }
    }

    private void HideReconInfo()
    {
        if (reconInfoText != null) reconInfoText.gameObject.SetActive(false);
    }

    public void UpdateMineAlert(int count)
    {
        if (mineAlertText != null)
        {
            mineAlertText.text = $"Warning: {count}";
            
            // 위험도에 따른 색상 변경
            if (count == 0) mineAlertText.color = Color.green;
            else if (count < 3) mineAlertText.color = Color.yellow;
            else mineAlertText.color = Color.red;
        }
    }

    public void ShowDirectionAlert(Vector3 playerPos, Vector3 targetPos, Color color)
    {
        if (directionIndicator != null)
        {
            directionIndicator.ShowDirection(playerPos, targetPos, color);
        }
    }

    public void UpdateHP(int currentHP)
    {
        for (int i = 0; i < hpIcons.Length; i++)
        {
            if (i < currentHP)
            {
                hpIcons[i].SetActive(true);
            }
            else
            {
                hpIcons[i].SetActive(false);
            }
        }
    }

    public void ShowResult(bool isWin, string message)
    {
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
            
            if (resultTitleText != null)
            {
                resultTitleText.text = isWin ? "VICTORY" : "DEFEAT";
                resultTitleText.color = isWin ? Color.cyan : Color.red;
            }

            if (resultMessageText != null)
            {
                resultMessageText.text = message;
            }
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
