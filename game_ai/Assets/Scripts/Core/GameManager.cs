using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameState currentState;

    [Header("References")]
    public UserController user;
    public AIController ai;
    private UserTurnActive userActive = new UserTurnActive();
    private void Reset()
    {
        user = FindFirstObjectByType<UserController>();
        ai = FindFirstObjectByType<AIController>();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        ChangeState(GameState.UserTurn);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Trạng thái hiện tại: " + currentState);

        switch (currentState)
        {
            case GameState.UserTurn:
                userActive.turnIndex++;
                userActive.active.Clear();

                ai.deck.ClearHand();
                user.deck.StartTurn();
                user.ResetStamina();
                break;

            case GameState.AITurn:
                user.deck.ClearHand();
                ai.ResetStamina();
                ai.StartTurnAI();
                break;
            case GameState.Win:
                Debug.Log("Chúc mừng! Bạn đã thắng.");
                break;
            case GameState.Loss:
                Debug.Log("Chia buồn! AI đã thắng.");
                break;
        }
    }
    public void EndTurn()
    {
        if (currentState == GameState.UserTurn)
            ChangeState(GameState.AITurn);
        else if (currentState == GameState.AITurn)
            ChangeState(GameState.UserTurn);
    }
    public bool HandleCardPlayed(CardData card)
    {
        if (currentState != GameState.UserTurn) return false;

        if (user.stamina < card.staminaCost)
        {
            Debug.Log("Không đủ thể lực!");
            return false;
        }

        user.ChangeStamina(-card.staminaCost);

        ExecuteEffect(card, user, ai);
        userActive.active.Add(card.type);

        if (user.stamina <= 0)
        {
            userActive.currentHp = user.currentHp;
            EndTurn();
        }

        return true;
    }

    public bool HandleCardAi(CardData card)
    {
        if (currentState != GameState.AITurn) return false;

        if (ai.stamina < card.staminaCost)
        {
            Debug.Log("Không đủ thể lực!");
            return false;
        }

        ai.ChangeStamina(-card.staminaCost);

        ExecuteEffect(card, ai, user);

        return true;
    }

    public void ExecuteEffect(CardData card, InfoAbstract attacker, InfoAbstract target)
    {
        switch (card.type)
        {
            case CardType.Attack:
            case CardType.StrongAttack:
                target.TakeDamage(card.value);
                break;
            case CardType.Heal:
                attacker.Heal(card.value);
                break;
            case CardType.StaminaUp:
                attacker.ChangeStamina(card.value);
                break;
        }

        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (user.currentHp <= 0) ChangeState(GameState.Loss);
        else if (ai.currentHp <= 0) ChangeState(GameState.Win);
    }

}
