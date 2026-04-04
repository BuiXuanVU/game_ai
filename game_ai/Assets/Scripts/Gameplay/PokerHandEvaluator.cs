using System.Collections.Generic;
using System.Linq;

public static class PokerHandEvaluator
{
    public static HandResult EvaluateHand(List<CardData> cards)
    {
        var combinations = GetCombinations(cards, 5);

        HandResult best = null;

        foreach (var combo in combinations)
        {
            var result = Evaluate5Cards(combo);

            if (best == null || CompareHands(result, best) > 0)
                best = result;
        }

        return best;
    }

    static HandResult Evaluate5Cards(List<CardData> cards)
    {
        var values = cards.Select(c => (int)c.rank).OrderByDescending(v => v).ToList();
        var groups = cards.GroupBy(c => c.rank)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key)
            .ToList();

        bool isFlush = cards.All(c => c.suit == cards[0].suit);

        // Straight
        var distinct = values.Distinct().ToList();
        if (distinct.Contains(14)) distinct.Add(1);

        bool isStraight = false;
        int straightHigh = 0;

        for (int i = 0; i <= distinct.Count - 5; i++)
        {
            if (distinct[i] - 1 == distinct[i + 1] &&
                distinct[i + 1] - 1 == distinct[i + 2] &&
                distinct[i + 2] - 1 == distinct[i + 3] &&
                distinct[i + 3] - 1 == distinct[i + 4])
            {
                isStraight = true;
                straightHigh = distinct[i];
                break;
            }
        }

        // =========================
        // CHECK HAND
        // =========================

        if (isStraight && isFlush)
            return new HandResult(HandRank.StraightFlush, new List<int> { straightHigh }, cards);

        if (groups[0].Count() == 4)
            return new HandResult(HandRank.FourOfAKind,
                new List<int> { (int)groups[0].Key, (int)groups[1].Key },
                cards);

        if (groups[0].Count() == 3 && groups[1].Count() == 2)
            return new HandResult(HandRank.FullHouse,
                new List<int> { (int)groups[0].Key, (int)groups[1].Key },
                cards);

        if (isFlush)
            return new HandResult(HandRank.Flush, values, cards);

        if (isStraight)
            return new HandResult(HandRank.Straight, new List<int> { straightHigh }, cards);

        if (groups[0].Count() == 3)
            return new HandResult(HandRank.ThreeOfAKind,
                new List<int> { (int)groups[0].Key }
                    .Concat(groups.Skip(1).Select(g => (int)g.Key)).ToList(),
                cards);

        if (groups[0].Count() == 2 && groups[1].Count() == 2)
            return new HandResult(HandRank.TwoPair,
                new List<int> {
                (int)groups[0].Key,
                (int)groups[1].Key,
                (int)groups[2].Key
                },
                cards);

        if (groups[0].Count() == 2)
            return new HandResult(HandRank.OnePair,
                new List<int> {
                (int)groups[0].Key
                }.Concat(groups.Skip(1).Select(g => (int)g.Key)).ToList(),
                cards);

        return new HandResult(HandRank.HighCard, values, cards);
    }

    private static List<List<CardData>> GetCombinations(List<CardData> list, int length)
    {
        var result = new List<List<CardData>>();
        Combine(list, new List<CardData>(), 0, length, result);
        return result;
    }

    private static void Combine(List<CardData> list, List<CardData> current, int start, int length, List<List<CardData>> result)
    {
        if (current.Count == length)
        {
            result.Add(new List<CardData>(current));
            return;
        }

        for (int i = start; i < list.Count; i++)
        {
            current.Add(list[i]);
            Combine(list, current, i + 1, length, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    public static int CompareHands(HandResult a, HandResult b)
    {
        if (a.Rank != b.Rank)
            return a.Rank.CompareTo(b.Rank);

        for (int i = 0; i < a.Values.Count; i++)
        {
            if (a.Values[i] != b.Values[i])
                return a.Values[i].CompareTo(b.Values[i]);
        }

        return 0;
    }
}
