from poker_agent.models import DecisionRequest


SYSTEM_PROMPT = """You are a disciplined Texas Hold'em poker assistant.
You must choose exactly one legal action for the current player.
Use only the legal actions and raise range provided by the input.
Be conservative when the situation is ambiguous.
If action is not Raise, amount must be 0.
Respond with JSON only using keys: action, amount, reason.
"""


def build_user_prompt(request: DecisionRequest) -> str:
    board = ", ".join(card.displayName for card in request.communityCards) or "None"
    my_cards = ", ".join(card.displayName for card in request.me.hand) or "Unknown"

    parts = [
        f"Phase: {request.phase}",
        f"Pot size: {request.potSize}",
        f"Highest bet: {request.highestBet}",
        f"Call amount: {request.callAmount}",
        f"Can fold: {request.canFold}",
        f"Can check: {request.canCheck}",
        f"Can call: {request.canCall}",
        f"Can raise: {request.canRaise}",
        f"Raise amount range: {request.minRaiseAmount} to {request.maxRaiseAmount}",
    ]

    if request.botProfile:
        parts.extend(
            [
                "",
                "[BOT PROFILE]",
                f"Name: {request.botProfile.name}",
                f"Style notes: {request.botProfile.styleNotes or 'None'}",
                f"History window: {request.botProfile.historyWindow}",
                f"Use opponent stats: {request.botProfile.useOpponentStats}",
            ]
        )

    parts.extend(
        [
            "",
            "[ME]",
            f"Name: {request.me.name}",
            f"Stack: {request.me.stack}",
            f"Current bet: {request.me.currentBet}",
            f"Cards: {my_cards}",
            "",
            "[BOARD]",
            board,
            "",
            "[OPPONENTS]",
        ]
    )

    if request.opponents:
        for opponent in request.opponents:
            parts.append(
                f"{opponent.name} | stack={opponent.stack} | bet={opponent.currentBet} | "
                f"folded={opponent.isFolded} | all_in={opponent.isAllIn}"
            )
    else:
        parts.append("None")

    parts.extend(["", "[OPPONENT STATS]"])
    if request.opponentStats:
        for stats in request.opponentStats:
            parts.append(
                f"{stats.name} | hands={stats.handsObserved} | fold={stats.foldRate:.2f} | "
                f"call={stats.callRate:.2f} | raise={stats.raiseRate:.2f} | "
                f"showdown_win={stats.showdownWinRate:.2f} | aggression={stats.aggressionScore:.2f}"
            )
    else:
        parts.append("None")

    parts.extend(["", "[ROUND HISTORY]"])
    if request.currentRoundHistory:
        for action in request.currentRoundHistory:
            parts.append(f"{action.phase}: {action.playerName} -> {action.action} ({action.amount})")
    else:
        parts.append("No actions yet.")

    parts.extend(["", "[RECENT MEMORY]"])
    if request.memory:
        for memory_item in request.memory:
            parts.append(
                f"Round {memory_item.roundNumber}: winner={memory_item.winnerName}, "
                f"pot={memory_item.finalPot}, summary={memory_item.summary}"
            )
    else:
        parts.append("No memory.")

    parts.extend(
        [
            "",
            "[TASK]",
            "Pick the best legal action for this spot.",
            "Return the decision in the structured response format.",
        ]
    )

    return "\n".join(parts)
