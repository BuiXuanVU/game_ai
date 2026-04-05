using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChipController : MonoBehaviour
{
    private CardDataManager cardDataManager;

    private List<Image> chipPool = new List<Image>();

    private int[] chipValues = { 5000, 1000, 500, 100, 25, 10, 5, 1 };

    void Start()
    {
        cardDataManager = CardDataManager.Instance;
    }

    // =========================
    // 🎯 MAIN
    // =========================
    public void RenderChips(int amount)
    {
        ClearChips();
        RectTransform rectTransform = GetComponent<RectTransform>();
        float startX = 0f;
        float startY = 0f;

        float spacingX = 40f;
        float spacingY = 30f;
        float stackOffsetY = 5f;

        int maxColumns = 3;

        var chipGroups = BreakDownAmount(amount);
        var groups = GetSplitGroups(chipGroups.Count);

        int index = 0;

        //rectTransform.sizeDelta = new Vector2(spacingX * groups.First(), spacingY * groups.Count);

        foreach (int group in groups)
        {
            float offsetX = (maxColumns - group) * (spacingX / 2f);
            float currentX = startX + offsetX;
            for (int i = 0; i < group; i++)
            {
                if (index >= chipGroups.Count)
                    break;

                float currentY = startY;

                var pair = chipGroups[index];
                int chipValue = pair.Key;
                int totalCount = pair.Value;

                for (int j = 0; j < totalCount; j++)
                {
                    CreateChip(currentX, currentY, chipValue);
                    currentY += stackOffsetY;
                }

                currentX += spacingX;
                index++;
            }

            startY -= spacingY;
        }
    }

    // =========================
    // 🧱 CREATE CHIP (POOL)
    // =========================
    private void CreateChip(float x, float y, int chipValue)
    {
        Image chip = GetChipFromPool();
        chip.rectTransform.anchoredPosition = new Vector2(x, y);
        chip.rectTransform.localScale = Vector3.one;
        chip.rectTransform.localRotation = Quaternion.identity;
        chip.sprite = cardDataManager.GetChip(chipValue);
    }

    // =========================
    // ♻️ POOL LOGIC
    // =========================
    private Image GetChipFromPool()
    {
        foreach (var chip in chipPool)
        {
            if (!chip.gameObject.activeInHierarchy)
            {
                chip.gameObject.SetActive(true);
                return chip;
            }
        }

        Image newChip = Instantiate(cardDataManager.chipPrefab, transform);
        chipPool.Add(newChip);
        return newChip;
    }

    private void ClearChips()
    {
        foreach (var chip in chipPool)
        {
            chip.gameObject.SetActive(false);
        }
    }

    // =========================
    // 🧠 LOGIC CHIP
    // =========================
    public List<KeyValuePair<int, int>> BreakDownAmount(int wallet)
    {
        Dictionary<int, int> layout = new Dictionary<int, int>();
        int remaining = wallet;

        var sortedValues = chipValues.OrderByDescending(v => v);

        foreach (int chip in sortedValues)
        {
            if (remaining >= chip)
            {
                int count = remaining / chip;
                layout.Add(chip, count);
                remaining %= chip;
            }
        }

        return layout
            .OrderByDescending(x => x.Value)
            .ThenByDescending(x => x.Key)
            .ToList();
    }

    private List<int> GetSplitGroups(int count)
    {
        switch (count)
        {
            case 1: return new List<int> { 1 };
            case 2: return new List<int> { 2 };
            case 3: return new List<int> { 3 };
            case 4: return new List<int> { 2, 2 };
            case 5: return new List<int> { 3, 2 };
            case 6: return new List<int> { 3, 3 };
            case 7: return new List<int> { 3, 2, 2 };
            case 8: return new List<int> { 3, 3, 2 };
            default: return new List<int> { 3, 3, 2 };
        }
    }

    public Vector2 GetScreenPosition()
    {
        RectTransform rt = GetComponent<RectTransform>();

        return RectTransformUtility.WorldToScreenPoint(null, rt.position);
    }
}