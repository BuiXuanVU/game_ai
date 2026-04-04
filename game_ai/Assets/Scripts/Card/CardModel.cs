using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class CardModel : MonoBehaviour
{
    [SerializeField] private Image SpriteRenderer;
    [SerializeField] private Animator animator;
    public CardData data;

    private void Reset()
    {
        SpriteRenderer = GetComponent<Image>();
        gameObject.SetActive(false);
        animator = GetComponent<Animator>();
        animator.enabled = false;
    }
    public void SetData(CardData newData)
    {
        this.data = newData;
        SpriteRenderer.sprite = data.back;
    }

    public void ShowCard(bool isVisible) => gameObject.SetActive(isVisible);

    public IEnumerator ActiveAnimation()
    {
        animator.enabled = true;
        gameObject.SetActive(true);
        yield return null;
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }
        animator.enabled = false; 
    }

    public void Flip()
    {
        StartCoroutine(FlipAnimation());
    }

    private IEnumerator FlipAnimation()
    {
        float duration = 0.2f;
        animator.enabled = false;
        RectTransform rect = GetComponent<RectTransform>();

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float scaleX = Mathf.Lerp(1, 0, t / duration);
            rect.localScale = new Vector3(scaleX, 1, 1);
            yield return null;
        }

        SpriteRenderer.sprite = data.face;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float scaleX = Mathf.Lerp(0, 1, t / duration);
            rect.localScale = new Vector3(scaleX, 1, 1);
            yield return null;
        }
    }
}