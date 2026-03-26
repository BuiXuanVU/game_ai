using UnityEngine;
using UnityEngine.UI;

public class EndTurnButton : MonoBehaviour
{
    public Button button;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        button.onClick.AddListener(EndTurn);
    }
    private void EndTurn()
    {
        GameManager.Instance.EndTurn();
    }
}
