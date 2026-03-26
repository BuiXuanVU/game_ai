from models import GameState

def build_prompt(state: GameState) -> str:
    lines = []

    lines.append("You are a smart card game AI.")

    lines.append(f"Your HP: {state.currentHp}")
    lines.append(f"Your Stamina: {state.stamina}")
    lines.append(f"Enemy HP: {state.enemyHp}")

    lines.append("Your hand:")

    for i, c in enumerate(state.hand):
        lines.append(f"{i}: {c.type} value {c.value} cost {c.staminaCost}")

    lines.append("Rules:")
    lines.append("- Prefer killing enemy if possible")
    lines.append("- Don't waste heal at full HP")
    lines.append("- Use strong cards wisely")

    lines.append("Return ONLY JSON:")
    lines.append('{ "cardIndex": number }')

    return "\n".join(lines)