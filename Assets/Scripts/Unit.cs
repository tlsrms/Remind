using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public HexCoordinates Location { get; private set; }
    
    [Header("Status")]
    public int maxHP = 3;
    public int currentHP;
    public bool hasRadio = false;
    public bool hasMask = false;

    // Movement animation speed
    [SerializeField]
    private float moveSpeed = 5f;

    public void Initialize(HexCoordinates startCoords, Vector3 startPosition)
    {
        Location = startCoords;
        transform.position = startPosition;
        gameObject.name = "Player Unit";
        currentHP = maxHP;
        hasRadio = false;
        hasMask = false;

        // UI 초기화
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHP(currentHP);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        Debug.Log($"Unit took {damage} damage! Current HP: {currentHP}");

        // UI 갱신
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHP(currentHP);
        }

        if (currentHP <= 0)
        {
            Debug.Log("Unit Died!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver(false, "You Died!\n(HP Depleted)");
            }
        }
    }

    public void MoveTo(HexCoordinates targetCoords, Vector3 targetPosition)
    {
        Location = targetCoords;
        StopAllCoroutines();
        StartCoroutine(AnimateMove(targetPosition));
    }

    private IEnumerator AnimateMove(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
    }
}
