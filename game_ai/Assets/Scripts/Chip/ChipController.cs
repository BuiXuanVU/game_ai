using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ChipController : MonoBehaviour
{
    private CardDataManager cardDataManager;
    [SerializeField] private List<Image> spawnedChips = new List<Image>();

    private int[] chipValues = { 5000, 1000, 500, 100, 25, 10, 5, 1 };
    public Image chipPrefab;
    public int wallet = 1000;

    void Start()
    {
        cardDataManager = CardDataManager.Instance;
        RenderChips(wallet);
    }

    public void RenderChips(int amount)
    {
        ClearChips();

        RectTransform rect = GetComponent<RectTransform>();
        float startX = rect.anchoredPosition.x;
        float startY = rect.anchoredPosition.y;

        var chipGroups = BreakDownAmount();
        var groups = GetSplitGroups(chipGroups.Count);

        int index = 0;
        float spacingX = 40f;
        int maxColumns = 3;

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
                    currentY += 5f;
                }

                currentX += 40f;
                index++;
            }

            startY -= 30f;
        }
    }

    private void CreateChip(float x, float y, int chipValue)
    {
        Image chip = Instantiate(chipPrefab, transform);
        chip.rectTransform.anchoredPosition = new Vector2(x, y);
        chip.sprite = cardDataManager.GetChip(chipValue);
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

    public List<KeyValuePair<int, int>> BreakDownAmount()
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

        return layout.OrderByDescending(x => x.Value)
                     .ThenByDescending(x => x.Key)
                     .ToList();
    }

    private void ClearChips()
    {
        foreach (var chip in spawnedChips) if (chip != null) Destroy(chip.gameObject);
        spawnedChips.Clear();
    }
}


