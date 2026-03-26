using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

public class AIBrain
{
    private AIController ai;

    public AIBrain(AIController ai)
    {
        this.ai = ai;
    }

    public IEnumerator DecideCard(List<CardController> hand, Action<CardController> onDone)
    {
        CardController result = null;

        yield return CallLLM(hand, (index) =>
        {
            if (index >= 0 && index < hand.Count)
            {
                Debug.Log(index);
                result = hand[index];
            }
            else
                result = ChooseBestCard(hand);
        });

        onDone?.Invoke(result);
    }

    private CardController ChooseBestCard(List<CardController> hand)
    {
        CardController best = null;
        int bestScore = int.MinValue;

        foreach (var card in hand)
        {
            var data = card.cardData;

            if (data.staminaCost > ai.stamina) continue;

            int score = 0;

            switch (data.type)
            {
                case CardType.Attack:
                    score += data.value;
                    break;

                case CardType.Heal:
                    if (ai.currentHp < 50)
                        score += data.value * 2;
                    break;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = card;
            }
        }

        return best;
    }

    IEnumerator CallLLM(List<CardController> hand, Action<int> onResult)
    {
        string apiKey = "";
        string url = $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}";

        string prompt = BuildPrompt(hand);

        string jsonBody = JsonUtility.ToJson(new GeminiRequest(prompt));

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gemini Error: " + request.error);
            onResult?.Invoke(-1);
        }
        else
        {
            string response = request.downloadHandler.text;
            Debug.Log("Gemini RAW: " + response);

            int index = ExtractIndexGemini(response);
            onResult?.Invoke(index);
        }
    }

    int ExtractIndexGemini(string json)
    {
        try
        {
            var data = JsonUtility.FromJson<GeminiResponse>(json);

            string text = data.candidates[0].content.parts[0].text;

            Debug.Log("Gemini text: " + text);

            int start = text.IndexOf("{");
            int end = text.LastIndexOf("}");

            if (start >= 0 && end > start)
            {
                string cleanJson = text.Substring(start, end - start + 1);

                var result = JsonUtility.FromJson<AIResponse>(cleanJson);
                return result.cardIndex;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Parse Gemini lỗi: " + e.Message);
        }

        return -1;
    }

    string BuildPrompt(List<CardController> hand)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("You are an AI playing a card game.");

        sb.AppendLine($"Your HP: {ai.currentHp}");
        sb.AppendLine($"Your Stamina: {ai.stamina}");
        sb.AppendLine($"Enemy HP: {GameManager.Instance.user.currentHp}");

        sb.AppendLine("Your hand:");

        for (int i = 0; i < hand.Count; i++)
        {
            var c = hand[i].cardData;
            sb.AppendLine($"{i}: {c.type} value {c.value} cost {c.staminaCost}");
        }

        sb.AppendLine("Return JSON: { \"cardIndex\": number }");

        return sb.ToString();
    }
}

[System.Serializable]
public class GeminiRequest
{
    public Content[] contents;

    public GeminiRequest(string prompt)
    {
        contents = new Content[]
        {
            new Content(prompt)
        };
    }
}

[System.Serializable]
public class Content
{
    public Part[] parts;

    public Content(string text)
    {
        parts = new Part[]
        {
            new Part(text)
        };
    }
}

[System.Serializable]
public class Part
{
    public string text;

    public Part(string text)
    {
        this.text = text;
    }
}

[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public ContentResponse content;
}

[System.Serializable]
public class ContentResponse
{
    public PartResponse[] parts;
}

[System.Serializable]
public class PartResponse
{
    public string text;
}

[System.Serializable]
public class AIResponse
{
    public int cardIndex;
}