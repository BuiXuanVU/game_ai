using System;
using UnityEngine;
using UnityEngine.UI;

public class ActionButtonsUI : MonoBehaviour
{
    [SerializeField] private Button foldBtn;
    [SerializeField] private Button callBtn;
    [SerializeField] private Button raiseBtn;
    [SerializeField] private Button checkBtn;

    public Action OnFold;
    public Action OnCall;
    public Action OnRaise;
    public Action OnCheck;

    private void Reset()
    {
        foldBtn = transform.GetChild(0).GetComponent<Button>();
        callBtn = transform.GetChild(1).GetComponent<Button>();
        raiseBtn = transform.GetChild(2).GetComponent<Button>();
        checkBtn = transform.GetChild(3).GetComponent<Button>();
    }

    private void Awake()
    {
        foldBtn.onClick.AddListener(() => OnFold?.Invoke());
        callBtn.onClick.AddListener(() => OnCall?.Invoke());
        raiseBtn.onClick.AddListener(() => OnRaise?.Invoke());
        checkBtn.onClick.AddListener(() => OnCheck?.Invoke());
    }

    public void Refresh(bool canFold, bool canCall, bool canCheck, bool canRaise)
    {
        foldBtn.gameObject.SetActive(canFold);
        callBtn.gameObject.SetActive(canCall);
        checkBtn.gameObject.SetActive(canCheck);
        raiseBtn.gameObject.SetActive(canRaise);
    }
}
