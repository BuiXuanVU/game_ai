using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class InfoAbstract : MonoBehaviour
{
    public const int maxHp = 100;
    public Slider hpBar;
    public TextMeshProUGUI staminaUI;
    public int currentHp = 100;
    public int stamina = 10;

    protected virtual void Reset()
    {
        hpBar = transform.GetComponentInChildren<Slider>();
        staminaUI = transform.GetComponentInChildren<TextMeshProUGUI>();
    }

    protected virtual void Start()
    {
        currentHp = maxHp;
        if (hpBar != null)
        {
            hpBar.maxValue = maxHp;
            hpBar.value = currentHp;
        }
    }

    public virtual void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateUI();
    }

    public virtual void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (hpBar != null) hpBar.value = currentHp;
    }
    public void ChangeStamina(int amount)
    {
        stamina += amount;
        staminaUI.text = stamina.ToString();
    }
    public void ResetStamina()
    {
        stamina = 10;
        staminaUI.text = stamina.ToString();
    }

}
//// Nên gửi ảnh cho ai tự phân tích 
//// Thêm chat để nói chuyện với user

