using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class PokerLocalStore
{
    private const string RootFolderName = "PokerAI";
    private const string HandHistoryFileName = "hand_history.json";
    private const string DecisionAuditFileName = "decision_audit.json";

    public static string RootPath => Path.Combine(Application.persistentDataPath, RootFolderName);
    public static string HandHistoryPath => Path.Combine(RootPath, HandHistoryFileName);
    public static string DecisionAuditPath => Path.Combine(RootPath, DecisionAuditFileName);

    public static List<HandHistory> LoadHandHistory()
    {
        EnsureRootFolder();
        if (!File.Exists(HandHistoryPath))
            return new List<HandHistory>();

        string json = File.ReadAllText(HandHistoryPath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<HandHistory>();

        HandHistoryCollection wrapper = JsonUtility.FromJson<HandHistoryCollection>(json);
        return wrapper?.items ?? new List<HandHistory>();
    }

    public static void SaveHandHistory(List<HandHistory> history)
    {
        EnsureRootFolder();
        HandHistoryCollection wrapper = new HandHistoryCollection
        {
            items = history ?? new List<HandHistory>()
        };
        File.WriteAllText(HandHistoryPath, JsonUtility.ToJson(wrapper, true));
    }

    public static void AppendDecisionAudit(DecisionAuditEntry entry)
    {
        EnsureRootFolder();

        DecisionAuditCollection wrapper;
        if (File.Exists(DecisionAuditPath))
        {
            string json = File.ReadAllText(DecisionAuditPath);
            wrapper = string.IsNullOrWhiteSpace(json)
                ? new DecisionAuditCollection()
                : JsonUtility.FromJson<DecisionAuditCollection>(json);
        }
        else
        {
            wrapper = new DecisionAuditCollection();
        }

        if (wrapper == null)
            wrapper = new DecisionAuditCollection();

        if (wrapper.items == null)
            wrapper.items = new List<DecisionAuditEntry>();

        wrapper.items.Add(entry);
        File.WriteAllText(DecisionAuditPath, JsonUtility.ToJson(wrapper, true));
    }

    private static void EnsureRootFolder()
    {
        Directory.CreateDirectory(RootPath);
    }
}
