from __future__ import annotations

import csv
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

from poker_agent.decision_service import DecisionService
from poker_agent.llm_runtime import LmDecisionClient
from poker_agent.models import DecisionRequest, DecisionResponse, OpponentStats

RANK_TO_TEXT = {2: "2", 3: "3", 4: "4", 5: "5", 6: "6", 7: "7", 8: "8", 9: "9", 10: "10", 11: "Jack", 12: "Queen", 13: "King", 14: "Ace"}
SUIT_TO_TEXT = {"C": "Clubs", "D": "Diamonds", "H": "Hearts", "S": "Spades"}
HAND_LABELS = {8: "Straight Flush", 7: "Four of a Kind", 6: "Full House", 5: "Flush", 4: "Straight", 3: "Three of a Kind", 2: "Two Pair", 1: "One Pair", 0: "High Card"}


def card_to_model(card: Tuple[int, str]):
    from poker_agent.models import CardData
    rank, suit = card
    return CardData(rank=RANK_TO_TEXT[rank], suit=SUIT_TO_TEXT[suit], displayName=f"{RANK_TO_TEXT[rank]} of {SUIT_TO_TEXT[suit]}")


@dataclass
class BotConfig:
    name: str
    policy: str
    history_window: int = 0
    use_opponent_stats: bool = False
    style_notes: str = ""
    model_name: str | None = None


@dataclass
class SimulationConfig:
    starting_stack: int = 1000
    small_blind: int = 10
    big_blind: int = 20
    hands_per_match: int = 100
    matches_per_pairing: int = 3
    seed: int = 7
    table_sizes: Tuple[int, ...] = (2, 3)


@dataclass
class BotTracker:
    action_counts: Dict[str, int] = field(default_factory=lambda: {"Fold": 0, "Check": 0, "Call": 0, "Raise": 0})
    hands_observed: int = 0
    showdown_wins: int = 0

    def record_action(self, action: str) -> None:
        self.action_counts[action] = self.action_counts.get(action, 0) + 1

    def build_stats(self, opponent_name: str) -> OpponentStats:
        total = max(1, sum(self.action_counts.values()))
        aggression = (self.action_counts.get("Raise", 0) + 0.5 * self.action_counts.get("Call", 0)) / total
        return OpponentStats(
            name=opponent_name,
            handsObserved=self.hands_observed,
            foldRate=self.action_counts.get("Fold", 0) / total,
            callRate=self.action_counts.get("Call", 0) / total,
            raiseRate=self.action_counts.get("Raise", 0) / total,
            showdownWinRate=self.showdown_wins / max(1, self.hands_observed),
            aggressionScore=aggression,
        )


class BaselinePolicy:
    def decide(self, request: DecisionRequest) -> DecisionResponse:
        hole_cards = [(self._to_rank(card.rank), self._to_suit(card.suit)) for card in request.me.hand]
        board_cards = [(self._to_rank(card.rank), self._to_suit(card.suit)) for card in request.communityCards]
        strength, label = summarize_strength(hole_cards, board_cards)
        pressure = request.callAmount / max(1, request.me.stack + request.callAmount)
        opponent_factor = 1.0 + 0.15 * max(0, len(request.opponents) - 1)
        adjusted = strength / opponent_factor
        reason = f"Baseline heuristic using {label} strength={strength:.2f}, pressure={pressure:.2f}, opponents={len(request.opponents)}."

        if request.callAmount == 0:
            if request.canRaise and self._should_value_bet(request.phase.lower(), label, adjusted):
                return DecisionResponse(action="Raise", amount=self._pick_raise(request, strength, pressure), reason=reason)
            return DecisionResponse(action="Check", amount=0, reason=reason)

        if self._should_fold(request.phase.lower(), label, adjusted, pressure) and request.canFold:
            return DecisionResponse(action="Fold", amount=0, reason=reason)
        if request.canRaise and self._should_raise_over_bet(request.phase.lower(), label, adjusted, pressure):
            return DecisionResponse(action="Raise", amount=self._pick_raise(request, strength, pressure), reason=reason)
        if request.canCall:
            return DecisionResponse(action="Call", amount=0, reason=reason)
        return DecisionResponse(action="Fold", amount=0, reason=reason)

    def _should_value_bet(self, stage: str, label: str, strength: float) -> bool:
        if stage == "preflop":
            return strength >= 0.92
        if label in {"Straight Flush", "Four of a Kind", "Full House", "Flush", "Straight", "Three of a Kind"}:
            return True
        if label == "Two Pair":
            return strength >= 0.75
        if label == "One Pair":
            return strength >= 0.90
        return False

    def _should_fold(self, stage: str, label: str, strength: float, pressure: float) -> bool:
        if stage == "preflop":
            return strength < 0.58 and pressure > 0.12
        if label == "High Card":
            return pressure > 0.08
        if label == "One Pair":
            return strength < 0.62 and pressure > 0.22
        return False

    def _should_raise_over_bet(self, stage: str, label: str, strength: float, pressure: float) -> bool:
        if pressure > 0.25:
            return False
        if stage == "preflop":
            return strength >= 0.98
        if label in {"Straight Flush", "Four of a Kind", "Full House", "Flush", "Straight"}:
            return True
        if label == "Three of a Kind":
            return pressure <= 0.18
        if label == "Two Pair":
            return strength >= 0.82 and pressure <= 0.12
        return False

    def _pick_raise(self, request: DecisionRequest, strength: float, pressure: float) -> int:
        if request.maxRaiseAmount <= 0:
            return 0
        if request.phase.lower() == "preflop":
            target = request.callAmount + max(request.minRaiseAmount, 2 * max(1, request.minRaiseAmount))
            return min(max(request.minRaiseAmount, target), request.maxRaiseAmount)
        target = max(request.minRaiseAmount, 2 * max(1, request.minRaiseAmount))
        if strength >= 0.90 and pressure < 0.10:
            target = max(target, 4 * max(1, request.minRaiseAmount))
        return min(target, request.maxRaiseAmount)

    def _to_rank(self, value: str | int) -> int:
        if isinstance(value, int):
            return value
        return {name: rank for rank, name in RANK_TO_TEXT.items()}[str(value)]

    def _to_suit(self, value: str | int) -> str:
        if isinstance(value, int):
            return "S"
        return {name: suit for suit, name in SUIT_TO_TEXT.items()}[str(value)]


class LlmPolicy:
    def __init__(self, model_name: str | None) -> None:
        self.service = DecisionService(LmDecisionClient(model_name=model_name))

    def decide(self, request: DecisionRequest) -> DecisionResponse:
        return self.service.decide(request)


class CsvLogger:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.root.mkdir(parents=True, exist_ok=True)
        self.match_file = self._open_writer("matches.csv", ["run_id", "match_id", "table_size", "players", "seed", "total_hands", "starting_stack", "small_blind", "big_blind", "final_stacks", "winner"])
        self.hand_file = self._open_writer("hands.csv", ["run_id", "match_id", "table_size", "hand_id", "dealer", "winner", "win_amount", "pot_size", "end_round", "showdown", "stacks_after"])
        self.decision_file = self._open_writer("decisions.csv", ["run_id", "match_id", "table_size", "hand_id", "decision_id", "player", "profile", "stage", "pot_size", "to_call", "player_chips", "chosen_action", "bet_amount", "response_time_ms", "history_window_size", "used_opponent_stats", "used_fallback", "reason"])

    def _open_writer(self, file_name: str, fieldnames: List[str]) -> Tuple[csv.DictWriter, object]:
        handle = (self.root / file_name).open("w", newline="", encoding="utf-8")
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        return writer, handle

    def write_match(self, row: dict) -> None:
        self.match_file[0].writerow(row)

    def write_hand(self, row: dict) -> None:
        self.hand_file[0].writerow(row)

    def write_decision(self, row: dict) -> None:
        self.decision_file[0].writerow(row)

    def close(self) -> None:
        for _, handle in (self.match_file, self.hand_file, self.decision_file):
            handle.close()


def summarize_strength(hole_cards: Sequence[Tuple[int, str]], board: Sequence[Tuple[int, str]]) -> Tuple[float, str]:
    if len(board) < 3:
        ranks = sorted((rank for rank, _ in hole_cards), reverse=True)
        suited = hole_cards[0][1] == hole_cards[1][1]
        connected = abs(hole_cards[0][0] - hole_cards[1][0]) <= 2
        score = 0.2 + (0.2 if suited else 0.0) + (0.1 if connected else 0.0)
        score += 0.4 + (ranks[0] / 20.0) if ranks[0] == ranks[1] else (ranks[0] + ranks[1]) / 30.0
        return min(score, 1.0), "preflop"
    rank, kickers = best_hand([*hole_cards, *board])
    return min((rank / 8.0) + ((sum(kickers[:2]) / 30.0) if kickers else 0.0), 1.0), HAND_LABELS[rank]


def best_hand(cards: Sequence[Tuple[int, str]]) -> Tuple[int, List[int]]:
    from itertools import combinations
    return max((evaluate_five(combo) for combo in combinations(cards, 5)), key=lambda item: (item[0], item[1]))


def build_deck(rng) -> List[Tuple[int, str]]:
    deck = [(rank, suit) for suit in ("C", "D", "H", "S") for rank in range(2, 15)]
    rng.shuffle(deck)
    return deck


def evaluate_five(cards: Sequence[Tuple[int, str]]) -> Tuple[int, List[int]]:
    ranks = sorted((rank for rank, _ in cards), reverse=True)
    suits = [suit for _, suit in cards]
    counts: Dict[int, int] = {}
    for rank in ranks:
        counts[rank] = counts.get(rank, 0) + 1
    ordered_counts = sorted(counts.items(), key=lambda item: (item[1], item[0]), reverse=True)
    is_flush = len(set(suits)) == 1
    straight = straight_high(ranks)
    if is_flush and straight is not None:
        return 8, [straight]
    if ordered_counts[0][1] == 4:
        return 7, [ordered_counts[0][0], max(rank for rank in ranks if rank != ordered_counts[0][0])]
    if ordered_counts[0][1] == 3 and ordered_counts[1][1] == 2:
        return 6, [ordered_counts[0][0], ordered_counts[1][0]]
    if is_flush:
        return 5, ranks
    if straight is not None:
        return 4, [straight]
    if ordered_counts[0][1] == 3:
        kickers = sorted((rank for rank in ranks if rank != ordered_counts[0][0]), reverse=True)
        return 3, [ordered_counts[0][0], *kickers]
    if ordered_counts[0][1] == 2 and ordered_counts[1][1] == 2:
        pair_ranks = sorted((ordered_counts[0][0], ordered_counts[1][0]), reverse=True)
        return 2, [*pair_ranks, max(rank for rank in ranks if rank not in pair_ranks)]
    if ordered_counts[0][1] == 2:
        pair_rank = ordered_counts[0][0]
        return 1, [pair_rank, *sorted((rank for rank in ranks if rank != pair_rank), reverse=True)]
    return 0, ranks


def straight_high(ranks: Sequence[int]) -> int | None:
    ordered = sorted(set(ranks), reverse=True)
    if 14 in ordered:
        ordered.append(1)
    run = 1
    best = None
    for idx in range(1, len(ordered)):
        if ordered[idx - 1] - 1 == ordered[idx]:
            run += 1
            if run >= 5:
                best = ordered[idx - 4]
        else:
            run = 1
    return best
