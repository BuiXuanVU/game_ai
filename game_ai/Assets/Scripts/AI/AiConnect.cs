using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AiConnect
{
    private AIController ai;

    public AiConnect(AIController ai)
    {
        this.ai = ai;
    }

    public IEnumerator Call(List<CardController> hand,Action<CardController> onDone)
    {
        CardController result = null;

        yield return CallBackend(hand, (index) =>
        {
            if (index >= 0 && index < hand.Count)
                result = hand[index];
        });

        onDone?.Invoke(result);
    }

    IEnumerator CallBackend(List<CardController> hand, Action<int> onResult)
    {
        string url = "http://127.0.0.1:8000/decide";

        AIRequest req = new AIRequest();
        req.currentHp = ai.currentHp;
        req.stamina = ai.stamina;
        req.enemyHp = GameManager.Instance.user.currentHp;

        req.hand = new List<CardDataSend>();

        foreach (var c in hand)
        {
            req.hand.Add(new CardDataSend
            {
                type = c.cardData.type.ToString(),
                value = c.cardData.value,
                staminaCost = c.cardData.staminaCost
            });
        }

        string json = JsonUtility.ToJson(req);
        Debug.Log("SEND JSON: " + json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Backend error: " + request.error);
            onResult?.Invoke(-1);
        }
        else
        {
            string resText = request.downloadHandler.text;
            Debug.Log("RESPONSE: " + resText);

            var res = JsonUtility.FromJson<AIResponse>(resText);
            onResult?.Invoke(res.cardIndex);
        }
    }
}
