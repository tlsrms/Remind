using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DirectionIndicator : MonoBehaviour
{
    public RectTransform arrowRect;
    public Image arrowImage;
    public float displayDuration = 2.0f; // 표시 시간

    private Coroutine currentRoutine;

    void Awake()
    {
        // 시작 시 숨김
        if (arrowImage != null)
        {
            Color c = arrowImage.color;
            c.a = 0;
            arrowImage.color = c;
        }
    }

    public void ShowDirection(Vector3 playerPos, Vector3 targetPos, Color color)
    {
        gameObject.SetActive(true);
        
        // 1. 각도 계산 (플레이어 -> 타겟)
        Vector3 direction = (targetPos - playerPos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 2. UI 회전 (스프라이트가 위쪽(Up)을 향해 있다고 가정 시 -90도 보정 필요할 수 있음)
        // 여기서는 스프라이트가 오른쪽(Right, 0도)을 향해 있다고 가정하거나, 
        // Unity UI 회전 기준(Z축)에 맞춰 설정. 화살표가 '위'를 보고 있다면 -90을 해줍니다.
        arrowRect.rotation = Quaternion.Euler(0, 0, angle - 90);

        // 3. 색상 설정 및 애니메이션 시작
        arrowImage.color = color;
        
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // Fade In
        float timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / 0.2f);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(1);

        // Wait
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / 0.5f);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0);
        gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        if (arrowImage != null)
        {
            Color c = arrowImage.color;
            c.a = alpha;
            arrowImage.color = c;
        }
    }
}
