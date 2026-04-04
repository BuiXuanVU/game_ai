using System;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RaiseSliderUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Button btnCancel;
    [SerializeField] private Button btnConfirm;

    public Action<int> OnConfirm;
    public Action OnCancel;

    private void Reset()
    {
        slider = GetComponent<Slider>();
        amountText = GetComponentInChildren<TextMeshProUGUI>();
        var btns = GetComponentsInChildren<Button>();
        btnCancel = btns.First();
        btnConfirm = btns.Last();
    }

    private void Awake()
    {
        btnCancel.onClick.AddListener(() =>
        {
            Hide();
            OnCancel?.Invoke();
        });

        btnConfirm.onClick.AddListener(() =>
        {
            OnConfirm?.Invoke(Value);
            Hide();
        });
    }

    public int Value => (int)slider.value;

    public void Setup(int min, int max)
    {
        gameObject.SetActive(true);

        slider.minValue = min;
        slider.maxValue = max;
        slider.value = min;

        UpdateText(min);

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(v => UpdateText((int)v));
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void UpdateText(int value)
    {
        if (amountText != null)
            amountText.text = value.ToString();
    }
}