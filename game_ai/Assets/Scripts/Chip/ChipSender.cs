using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ChipSender : MonoBehaviour
{
    public List<Image> chipPool = new List<Image>();

    private CardDataManager cardDataManager;

    private void Reset()
    {
        chipPool.AddRange(GetComponentsInChildren<Image>());
    }

    void Awake()
    {
        cardDataManager = CardDataManager.Instance;
        foreach (var chip in chipPool)
        {
            chip.gameObject.SetActive(false);
        }
    }

    // =========================
    // 🎯 MAIN
    // =========================

    public IEnumerator SendRoutine(ChipController from, ChipController to, int amount)
    {
        Vector2 start = ScreenToCanvas(from.GetScreenPosition());
        Vector2 end = ScreenToCanvas(to.GetScreenPosition());

        int chipCount = Mathf.Min(chipPool.Count, 10);

        List<Coroutine> coroutines = new List<Coroutine>();

        for (int i = 0; i < chipCount; i++)
        {
            Image chip = GetFreeChip();
            if (chip == null) break;

            SetupChip(chip, start);
            chip.sprite = GetRandomChipSprite();

            float delay = i * 0.05f;

            coroutines.Add(
                StartCoroutine(MoveChipWithDelay(chip, start, end, 0.5f, delay))
            );
        }

        yield return new WaitForSeconds(0.5f + chipCount * 0.05f);
    }

    private Vector2 ScreenToCanvas(Vector2 screenPos)
    {
        RectTransform canvasRect = chipPool[0].canvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out Vector2 localPoint
        );

        return localPoint;
    }

    // =========================
    // 🎬 ANIMATION
    // =========================
    private IEnumerator MoveChipWithDelay(Image chip, Vector2 start, Vector2 end, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);

        yield return MoveChip(chip, start, end, duration);

        chip.gameObject.SetActive(false);
    }

    private IEnumerator MoveChip(Image chip, Vector2 start, Vector2 end, float duration)
    {
        float time = 0;
        RectTransform rt = chip.rectTransform;

        float height = Random.Range(80f, 150f);
        float offsetX = Random.Range(-30f, 30f);

        while (time < duration)
        {
            float t = time / duration;
            float easeT = 1 - Mathf.Pow(1 - t, 3);

            Vector2 pos = Vector2.Lerp(start, end, easeT);

            pos.y += height * Mathf.Sin(easeT * Mathf.PI);
            pos.x += offsetX * Mathf.Sin(easeT * Mathf.PI);

            rt.anchoredPosition = pos;

            float scale = Mathf.Lerp(0.8f, 1.1f, easeT);
            rt.localScale = Vector3.one * scale;

            time += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = end;
    }

    // =========================
    // 🧱 HELPER
    // =========================
    private void SetupChip(Image chip, Vector2 start)
    {
        RectTransform rt = chip.rectTransform;

        chip.gameObject.SetActive(true);
        rt.anchoredPosition = start;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private Image GetFreeChip()
    {
        foreach (var chip in chipPool)
        {
            if (!chip.gameObject.activeInHierarchy)
                return chip;
        }
        return null;
    }

    private Sprite GetRandomChipSprite()
    {
        int[] values = { 1000, 500, 100 };
        int value = values[Random.Range(0, values.Length)];
        return cardDataManager.GetChip(value);
    }
}