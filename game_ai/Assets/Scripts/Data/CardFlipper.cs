using System.Collections;
using UnityEngine;

public class CardFlipper : MonoBehaviour
{
    public CardModel cardModel;

    public AnimationCurve scaleCurve;
    public float duration = 0.5f;

    private void Reset()
    {
        cardModel = GetComponent<CardModel>();
    }

    public void FlipCard(Sprite start, Sprite end, int cardIndex)
    {
        StopCoroutine(Flip(start,end, cardIndex));
        StartCoroutine(Flip(start,end, cardIndex));

    }

    IEnumerator Flip(Sprite start, Sprite end, int cardIndex)
    {
        SpriteRenderer spriteRenderer =  cardModel.spriteRenderer;
        spriteRenderer.sprite = start;

        float time = 0f;
        while (time <= 1f)
        {
            float scale = scaleCurve.Evaluate(time);
            time += Time.deltaTime / duration;

            Vector3 localScale = transform.localScale;
            localScale.x = scale;
            transform.localScale = localScale;
            if(time >= 0.5f)
            {
                spriteRenderer.sprite = end;
            }

            yield return new WaitForFixedUpdate();
        }
        if(cardIndex == -1)
        {
            cardModel.ToggleFace(false);
        }
        else
        {
            cardModel.index = cardIndex;
            cardModel.ToggleFace(true);
        }

    }

}
