from __future__ import annotations

import csv
import itertools
import json
import random
import time
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

from poker_agent.decision_service import DecisionService
from poker_agent.llm_runtime import LmDecisionClient
from poker_agent.models import (
    ActionRecord,
    BotProfile,
    CardData,
    DecisionRequest,
    DecisionResponse,
    HandMemory,
    OpponentStats,
    PlayerState,
)

RANK_TO_TEXT = {
    2: "2",
    3: "3",
    4: "4",
    5: "5",
    6: "6",
    7: "7",
    8: "8",
    9: "9",
    10: "10",
    11: "Jack",
    12: "Queen",
    13: "King",
    14: "Ace",
}
SUIT_TO_TEXT = {
    "C": "Clubs",
    "D": "Diamonds",
    "H": "Hearts",
    "S": "Spades",
}
HAND_LABELS = {
    8: "Straight Flush",
    7: "Four of a Kind",
    6: "Full House",
    5: "Flush",
    4: "Straight",
    3: "Three of a Kind",
    2: "Two Pair",
    1: "One Pair",
    0: "High Card",
}


def card_to_model(card: Tuple[int, str]) -> CardData:
    rank, suit = card
    return CardData(rank=RANK_TO_TEXT[rank], suit=SUIT_TO_TEXT[suit], displayName=f"{RANK_TO_TEXT[rank]} of {SUIT_TO_TEXT[suit]}")


def build_deck(rng: random.Random) -> List[Tuple[int, str]]:
    deck = [(rank, suit) for suit in ("C", "D", "H", "S") for rank in range(2, 15)]
    rng.shuffle(deck)
    return deck


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
        kicker = max(rank for rank in ranks if rank != ordered_counts[0][0])
        return 7, [ordered_counts[0][0], kicker]
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
        kicker = max(rank for rank in ranks if rank not in pair_ranks)
        return 2, [*pair_ranks, kicker]
    if ordered_counts[0][1] == 2:
        pair_rank = ordered_counts[0][0]
        kickers = sorted((rank for rank in ranks if rank != pair_rank), reverse=True)
        return 1, [pair_rank, *kickers]
    return 0, ranks


def best_hand(cards: Sequence[Tuple[int, str]]) -> Tuple[int, List[int]]:
    return max((evaluate_five(combo) for combo in itertools.combinations(cards, 5)), key=lambda item: (item[0], item[1]))


def summarize_strength(hole_cards: Sequence[Tuple[int, str]], board: Sequence[Tuple[int, str]]) -> Tuple[float, str]:
    if len(board) < 3:
        ranks = sorted((rank for rank, _ in hole_cards), reverse=True)
        suited = hole_cards[0][1] == hole_cards[1][1]
        connected = abs(hole_cards[0][0] - hole_cards[1][0]) <= 2
        score = 0.2 + (0.2 if suited else 0.0) + (0.1 if connected else 0.0)
        if ranks[0] == ranks[1]:
            score += 0.4 + (ranks[0] / 20.0)
        else:
            score += (ranks[0] + ranks[1]) / 30.0
        return min(score, 1.0), "preflop"

    rank, kickers = best_hand([*hole_cards, *board])
    base = rank / 8.0
    kicker_bonus = (sum(kickers[:2]) / 30.0) if kickers else 0.0
    return min(base + kicker_bonus, 1.0), HAND_LABELS[rank]


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
        hands = max(1, self.hands_observed)
        return OpponentStats(
            name=opponent_name,
            handsObserved=self.hands_observed,
            foldRate=self.action_counts.get("Fold", 0) / total,
            callRate=self.action_counts.get("Call", 0) / total,
            raiseRate=self.action_counts.get("Raise", 0) / total,
            showdownWinRate=self.showdown_wins / hands,
            aggressionScore=aggression,
        )


class BaselinePolicy:
    def decide(self, request: DecisionRequest) -> DecisionResponse:
        board_cards = [(self._to_rank(card.rank), self._to_suit(card.suit)) for card in request.communityCards]
        hole_cards = [(self._to_rank(card.rank), self._to_suit(card.suit)) for card in request.me.hand]
        strength, label = summarize_strength(hole_cards, board_cards)
        pressure = request.callAmount / max(1, request.me.stack + request.callAmount)
        stage = request.phase.lower()
        reason = f"Baseline heuristic using {label} strength={strength:.2f}, pressure={pressure:.2f}."

        if request.callAmount == 0:
            if request.canRaise and self._should_value_bet(stage, label, strength):
                raise_amount = self._pick_raise(request, stage, strength, pressure)
                return DecisionResponse(action="Raise", amount=raise_amount, reason=reason)
            return DecisionResponse(action="Check", amount=0, reason=reason)

        if self._should_fold(stage, label, strength, pressure) and request.canFold:
            return DecisionResponse(action="Fold", amount=0, reason=reason)

        if request.canRaise and self._should_raise_over_bet(stage, label, strength, pressure):
            raise_amount = self._pick_raise(request, stage, strength, pressure)
            return DecisionResponse(action="Raise", amount=raise_amount, reason=reason)

        if request.canCall:
            return DecisionResponse(action="Call", amount=0, reason=reason)
        return DecisionResponse(action="Fold", amount=0, reason=reason)

    def _should_value_bet(self, stage: str, label: str, strength: float) -> bool:
        if stage == "preflop":
            return strength >= 0.9
        if label in {"Straight Flush", "Four of a Kind", "Full House", "Flush", "Straight"}:
            return True
        if label == "Three of a Kind":
            return True
        if label == "Two Pair":
            return strength >= 0.72
        if label == "One Pair":
            return strength >= 0.88
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
            return strength >= 0.97
        if label in {"Straight Flush", "Four of a Kind", "Full House", "Flush", "Straight"}:
            return True
        if label == "Three of a Kind":
            return pressure <= 0.18
        if label == "Two Pair":
            return strength >= 0.8 and pressure <= 0.12
        return False

    def _pick_raise(self, request: DecisionRequest, stage: str, strength: float, pressure: float) -> int:
        if request.maxRaiseAmount <= 0:
            return 0

        if stage == "preflop":
            target = max(request.minRaiseAmount, min(request.maxRaiseAmount, request.callAmount + self._blind_multiple(request, 2)))
            return target

        base = max(request.minRaiseAmount, self._blind_multiple(request, 2))
        if strength >= 0.9 and pressure < 0.1:
            target = max(base, self._blind_multiple(request, 4))
        else:
            target = base
        return min(target, request.maxRaiseAmount)

    def _blind_multiple(self, request: DecisionRequest, multiple: int) -> int:
        blind_unit = max(1, request.minRaiseAmount)
        return blind_unit * multiple

    def _to_rank(self, value: str | int) -> int:
        if isinstance(value, int):
            return value
        reverse = {name: rank for rank, name in RANK_TO_TEXT.items()}
        return reverse[str(value)]

    def _to_suit(self, value: str | int) -> str:
        if isinstance(value, int):
            return "S"
        reverse = {name: suit for suit, name in SUIT_TO_TEXT.items()}
        return reverse[str(value)]


class LlmPolicy:
    def __init__(self, model_name: str | None) -> None:
        self.service = DecisionService(LmDecisionClient(model_name=model_name))

    def decide(self, request: DecisionRequest) -> DecisionResponse:
        return self.service.decide(request)


class CsvLogger:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.root.mkdir(parents=True, exist_ok=True)
        self.match_file = self._open_writer(
            "matches.csv",
            [
                "run_id",
                "match_id",
                "bot_a",
                "bot_b",
                "seed",
                "total_hands",
                "starting_stack",
                "small_blind",
                "big_blind",
                "final_chip_a",
                "final_chip_b",
                "winner",
            ],
        )
        self.hand_file = self._open_writer(
            "hands.csv",
            [
                "run_id",
                "match_id",
                "hand_id",
                "dealer",
                "winner",
                "win_amount",
                "pot_size",
                "end_round",
                "showdown",
                "chip_a_after",
                "chip_b_after",
            ],
        )
        self.decision_file = self._open_writer(
            "decisions.csv",
            [
                "run_id",
                "match_id",
                "hand_id",
                "decision_id",
                "player",
                "profile",
                "stage",
                "pot_size",
                "to_call",
                "player_chips",
                "chosen_action",
                "bet_amount",
                "response_time_ms",
                "history_window_size",
                "used_opponent_stats",
                "used_fallback",
                "reason",
            ],
        )

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


class PokerSimulation:
    def __init__(self, bots: Sequence[BotConfig], config: SimulationConfig, output_root: Path) -> None:
        if len(bots) < 2:
            raise ValueError("At least two bot configs are required.")
        self.bots = list(bots)
        self.config = config
        self.run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        self.output_root = output_root / self.run_id
        self.logger = CsvLogger(self.output_root)
        self.policies = {
            bot.name: (BaselinePolicy() if bot.policy == "baseline" else LlmPolicy(bot.model_name))
            for bot in self.bots
        }

    def run_round_robin(self) -> Path:
        summary = {
            "run_id": self.run_id,
            "created_utc": datetime.now(timezone.utc).isoformat(),
            "config": self.config.__dict__,
            "bots": [bot.__dict__ for bot in self.bots],
        }
        (self.output_root / "run_config.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")

        matchup_index = 0
        for bot_a, bot_b in itertools.combinations(self.bots, 2):
            for match_number in range(self.config.matches_per_pairing):
                matchup_index += 1
                match_seed = self.config.seed + matchup_index * 1000 + match_number
                self._run_match(bot_a, bot_b, matchup_index, match_seed)

        self.logger.close()
        return self.output_root

    def _run_match(self, bot_a: BotConfig, bot_b: BotConfig, match_index: int, match_seed: int) -> None:
        stacks = {bot_a.name: self.config.starting_stack, bot_b.name: self.config.starting_stack}
        memories = {bot_a.name: [], bot_b.name: []}
        trackers = {bot_a.name: BotTracker(), bot_b.name: BotTracker()}
        rng = random.Random(match_seed)
        match_id = f"match_{match_index:03d}_{bot_a.name}_vs_{bot_b.name}"
        hands_played = 0

        for hand_number in range(1, self.config.hands_per_match + 1):
            if min(stacks.values()) <= 0:
                break
            dealer_name = bot_a.name if hand_number % 2 == 1 else bot_b.name
            self._run_hand(
                match_id=match_id,
                hand_id=hand_number,
                rng=rng,
                bot_a=bot_a,
                bot_b=bot_b,
                dealer_name=dealer_name,
                stacks=stacks,
                memories=memories,
                trackers=trackers,
            )
            hands_played = hand_number

        winner = bot_a.name if stacks[bot_a.name] > stacks[bot_b.name] else bot_b.name
        if stacks[bot_a.name] == stacks[bot_b.name]:
            winner = "Draw"
        self.logger.write_match(
            {
                "run_id": self.run_id,
                "match_id": match_id,
                "bot_a": bot_a.name,
                "bot_b": bot_b.name,
                "seed": match_seed,
                "total_hands": hands_played,
                "starting_stack": self.config.starting_stack,
                "small_blind": self.config.small_blind,
                "big_blind": self.config.big_blind,
                "final_chip_a": stacks[bot_a.name],
                "final_chip_b": stacks[bot_b.name],
                "winner": winner,
            }
        )

    def _run_hand(
        self,
        match_id: str,
        hand_id: int,
        rng: random.Random,
        bot_a: BotConfig,
        bot_b: BotConfig,
        dealer_name: str,
        stacks: Dict[str, int],
        memories: Dict[str, List[HandMemory]],
        trackers: Dict[str, BotTracker],
    ) -> None:
        order = [bot_a.name, bot_b.name]
        blind_order = [dealer_name, bot_b.name if dealer_name == bot_a.name else bot_a.name]
        board: List[Tuple[int, str]] = []
        deck = build_deck(rng)
        hole_cards = {
            bot_a.name: [deck.pop(), deck.pop()],
            bot_b.name: [deck.pop(), deck.pop()],
        }
        current_bets = {bot_a.name: 0, bot_b.name: 0}
        folded = {bot_a.name: False, bot_b.name: False}
        round_history: List[ActionRecord] = []
        pot = 0

        sb_player, bb_player = blind_order
        sb_paid = min(self.config.small_blind, stacks[sb_player])
        bb_paid = min(self.config.big_blind, stacks[bb_player])
        stacks[sb_player] -= sb_paid
        stacks[bb_player] -= bb_paid
        current_bets[sb_player] += sb_paid
        current_bets[bb_player] += bb_paid
        pot += sb_paid + bb_paid
        highest_bet = current_bets[bb_player]
        end_round = "PreFlop"
        showdown = False
        decision_id = 0

        hand_over, pot, highest_bet, decision_id = self._play_street(
            stage="PreFlop",
            acting_order=[dealer_name, bb_player],
            bot_configs={bot_a.name: bot_a, bot_b.name: bot_b},
            stacks=stacks,
            hole_cards=hole_cards,
            board=board,
            current_bets=current_bets,
            folded=folded,
            round_history=round_history,
            memories=memories,
            trackers=trackers,
            pot=pot,
            highest_bet=highest_bet,
            match_id=match_id,
            hand_id=hand_id,
            decision_id=decision_id,
        )

        if not hand_over:
            for stage, cards_to_draw, acting_order in (
                ("Flop", 3, [bb_player, dealer_name]),
                ("Turn", 1, [bb_player, dealer_name]),
                ("River", 1, [bb_player, dealer_name]),
            ):
                if sum(not folded[name] for name in order) <= 1:
                    hand_over = True
                    break
                board.extend(deck.pop() for _ in range(cards_to_draw))
                current_bets = {bot_a.name: 0, bot_b.name: 0}
                highest_bet = 0
                end_round = stage
                if any(stacks[name] == 0 for name in order if not folded[name]):
                    continue
                hand_over, pot, highest_bet, decision_id = self._play_street(
                    stage=stage,
                    acting_order=acting_order,
                    bot_configs={bot_a.name: bot_a, bot_b.name: bot_b},
                    stacks=stacks,
                    hole_cards=hole_cards,
                    board=board,
                    current_bets=current_bets,
                    folded=folded,
                    round_history=round_history,
                    memories=memories,
                    trackers=trackers,
                    pot=pot,
                    highest_bet=highest_bet,
                    match_id=match_id,
                    hand_id=hand_id,
                    decision_id=decision_id,
                )

        if sum(not folded[name] for name in order) == 1:
            winners = [next(name for name in order if not folded[name])]
        else:
            showdown = True
            while len(board) < 5:
                board.append(deck.pop())
            scores = {
                name: best_hand([*hole_cards[name], *board])
                for name in order
                if not folded[name]
            }
            best_score = max(scores.values())
            winners = [name for name, score in scores.items() if score == best_score]
            for winner_name in winners:
                trackers[winner_name].showdown_wins += 1
            end_round = "Showdown"

        share = pot // len(winners)
        remainder = pot % len(winners)
        for index, winner_name in enumerate(winners):
            stacks[winner_name] += share + (1 if index < remainder else 0)
        for tracker in trackers.values():
            tracker.hands_observed += 1

        winner_label = ", ".join(winners)
        summary = f"{winner_label} won {pot} chips in {end_round}."
        for bot_name in order:
            memories[bot_name].append(
                HandMemory(
                    roundNumber=hand_id,
                    winnerName=winner_label,
                    finalPot=pot,
                    summary=summary,
                )
            )

        self.logger.write_hand(
            {
                "run_id": self.run_id,
                "match_id": match_id,
                "hand_id": hand_id,
                "dealer": dealer_name,
                "winner": winner_label,
                "win_amount": pot,
                "pot_size": pot,
                "end_round": end_round,
                "showdown": showdown,
                "chip_a_after": stacks[bot_a.name],
                "chip_b_after": stacks[bot_b.name],
            }
        )

    def _play_street(
        self,
        stage: str,
        acting_order: List[str],
        bot_configs: Dict[str, BotConfig],
        stacks: Dict[str, int],
        hole_cards: Dict[str, List[Tuple[int, str]]],
        board: List[Tuple[int, str]],
        current_bets: Dict[str, int],
        folded: Dict[str, bool],
        round_history: List[ActionRecord],
        memories: Dict[str, List[HandMemory]],
        trackers: Dict[str, BotTracker],
        pot: int,
        highest_bet: int,
        match_id: str,
        hand_id: int,
        decision_id: int,
    ) -> Tuple[bool, int, int, int]:
        first_player, second_player = acting_order
        hand_over, pot, highest_bet, decision_id = self._single_action(
            stage,
            first_player,
            bot_configs,
            stacks,
            hole_cards,
            board,
            current_bets,
            folded,
            round_history,
            memories,
            trackers,
            pot,
            highest_bet,
            match_id,
            hand_id,
            decision_id,
        )
        if hand_over or folded[first_player]:
            return True, pot, highest_bet, decision_id

        hand_over, pot, highest_bet, decision_id = self._single_action(
            stage,
            second_player,
            bot_configs,
            stacks,
            hole_cards,
            board,
            current_bets,
            folded,
            round_history,
            memories,
            trackers,
            pot,
            highest_bet,
            match_id,
            hand_id,
            decision_id,
        )
        if hand_over or folded[second_player]:
            return True, pot, highest_bet, decision_id

        if current_bets[first_player] != current_bets[second_player]:
            hand_over, pot, highest_bet, decision_id = self._single_action(
                stage,
                first_player,
                bot_configs,
                stacks,
                hole_cards,
                board,
                current_bets,
                folded,
                round_history,
                memories,
                trackers,
                pot,
                highest_bet,
                match_id,
                hand_id,
                decision_id,
            )
            if hand_over:
                return True, pot, highest_bet, decision_id

        return False, pot, highest_bet, decision_id

    def _single_action(
        self,
        stage: str,
        player_name: str,
        bot_configs: Dict[str, BotConfig],
        stacks: Dict[str, int],
        hole_cards: Dict[str, List[Tuple[int, str]]],
        board: List[Tuple[int, str]],
        current_bets: Dict[str, int],
        folded: Dict[str, bool],
        round_history: List[ActionRecord],
        memories: Dict[str, List[HandMemory]],
        trackers: Dict[str, BotTracker],
        pot: int,
        highest_bet: int,
        match_id: str,
        hand_id: int,
        decision_id: int,
    ) -> Tuple[bool, int, int, int]:
        if folded[player_name] or stacks[player_name] == 0:
            return False, pot, highest_bet, decision_id

        opponent_name = next(name for name in bot_configs if name != player_name)
        to_call = max(0, highest_bet - current_bets[player_name])
        can_check = to_call == 0
        can_call = to_call > 0 and stacks[player_name] > 0
        can_raise = stacks[player_name] > to_call
        min_raise = self.config.big_blind if can_raise else 0
        max_raise = max(0, stacks[player_name] - to_call)
        if min_raise > max_raise:
            can_raise = False
            min_raise = 0
            max_raise = 0

        history_window = bot_configs[player_name].history_window
        memory_items = memories[player_name][-history_window:] if history_window > 0 else []

        request = DecisionRequest(
            phase=stage,
            potSize=pot,
            highestBet=highest_bet,
            callAmount=to_call,
            minRaiseAmount=min_raise,
            maxRaiseAmount=max_raise,
            canFold=True,
            canCheck=can_check,
            canCall=can_call,
            canRaise=can_raise,
            me=PlayerState(
                name=player_name,
                stack=stacks[player_name],
                currentBet=current_bets[player_name],
                isFolded=False,
                isAllIn=stacks[player_name] == 0,
                hand=[card_to_model(card) for card in hole_cards[player_name]],
            ),
            opponents=[
                PlayerState(
                    name=opponent_name,
                    stack=stacks[opponent_name],
                    currentBet=current_bets[opponent_name],
                    isFolded=folded[opponent_name],
                    isAllIn=stacks[opponent_name] == 0,
                    hand=[],
                )
            ],
            communityCards=[card_to_model(card) for card in board],
            currentRoundHistory=list(round_history),
            memory=memory_items,
            opponentStats=[trackers[opponent_name].build_stats(opponent_name)] if bot_configs[player_name].use_opponent_stats else [],
            botProfile=BotProfile(
                name=bot_configs[player_name].name,
                styleNotes=bot_configs[player_name].style_notes,
                historyWindow=bot_configs[player_name].history_window,
                useOpponentStats=bot_configs[player_name].use_opponent_stats,
                modelName=bot_configs[player_name].model_name,
            ),
        )

        started = time.perf_counter()
        decision = self.policies[player_name].decide(request)
        elapsed_ms = round((time.perf_counter() - started) * 1000.0, 3)
        action = decision.action.capitalize()
        amount = max(0, decision.amount)

        if action == "Fold":
            folded[player_name] = True
        elif action == "Call":
            paid = min(to_call, stacks[player_name])
            stacks[player_name] -= paid
            current_bets[player_name] += paid
            pot += paid
        elif action == "Raise":
            raise_amount = max(min_raise, min(amount, max_raise))
            total_commit = to_call + raise_amount
            paid = min(total_commit, stacks[player_name])
            stacks[player_name] -= paid
            current_bets[player_name] += paid
            highest_bet = current_bets[player_name]
            amount = raise_amount
            pot += paid
        else:
            action = "Check"
            amount = 0

        trackers[player_name].record_action(action)
        round_history.append(ActionRecord(phase=stage, playerName=player_name, action=action, amount=amount))
        decision_id += 1
        self.logger.write_decision(
            {
                "run_id": self.run_id,
                "match_id": match_id,
                "hand_id": hand_id,
                "decision_id": decision_id,
                "player": player_name,
                "profile": bot_configs[player_name].name,
                "stage": stage,
                "pot_size": pot,
                "to_call": to_call,
                "player_chips": stacks[player_name],
                "chosen_action": action,
                "bet_amount": amount,
                "response_time_ms": elapsed_ms,
                "history_window_size": bot_configs[player_name].history_window,
                "used_opponent_stats": bot_configs[player_name].use_opponent_stats,
                "used_fallback": decision.usedFallback,
                "reason": decision.reason,
            }
        )
        return sum(not folded[name] for name in bot_configs) <= 1, pot, highest_bet, decision_id
