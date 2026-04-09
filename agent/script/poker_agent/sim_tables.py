from __future__ import annotations

import itertools
import json
import random
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

from poker_agent.models import ActionRecord, BotProfile, DecisionRequest, HandMemory, PlayerState
from poker_agent.sim_support import BotConfig, BotTracker, CsvLogger, BaselinePolicy, LlmPolicy, SimulationConfig, best_hand, build_deck, card_to_model


class PokerSimulation:
    def __init__(self, bots: Sequence[BotConfig], config: SimulationConfig, output_root: Path) -> None:
        if len(bots) < 2:
            raise ValueError("At least two bot configs are required.")
        self.bots = list(bots)
        self.config = config
        self.run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        self.output_root = output_root / self.run_id
        self.logger = CsvLogger(self.output_root)
        self.policies = {bot.name: (BaselinePolicy() if bot.policy == "baseline" else LlmPolicy(bot.model_name)) for bot in self.bots}
        self.total_matches = 0
        self.completed_matches = 0
        self._last_progress_len = 0

    def run_tables(self) -> Path:
        summary = {"run_id": self.run_id, "created_utc": datetime.now(timezone.utc).isoformat(), "config": self.config.__dict__, "bots": [bot.__dict__ for bot in self.bots]}
        (self.output_root / "run_config.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
        self.total_matches = sum(
            self.config.matches_per_pairing
            for table_size in self.config.table_sizes
            if 2 <= table_size <= 3 and table_size <= len(self.bots)
            for _ in itertools.combinations(self.bots, table_size)
        )
        matchup_index = 0
        for table_size in self.config.table_sizes:
            if table_size < 2 or table_size > 3 or table_size > len(self.bots):
                continue
            for lineup in itertools.combinations(self.bots, table_size):
                for match_number in range(self.config.matches_per_pairing):
                    matchup_index += 1
                    seed = self.config.seed + matchup_index * 1000 + match_number
                    self._run_match(list(lineup), matchup_index, seed)
        self.logger.close()
        if self.total_matches:
            sys.stdout.write("\n")
            sys.stdout.flush()
        return self.output_root

    def _run_match(self, lineup: List[BotConfig], match_index: int, match_seed: int) -> None:
        names = [bot.name for bot in lineup]
        scores = {name: 0 for name in names}
        memories = {name: [] for name in names}
        trackers = {name: BotTracker() for name in names}
        rng = random.Random(match_seed)
        match_id = f"match_{match_index:03d}_{'_vs_'.join(names)}"
        hands_played = 0

        for hand_number in range(1, self.config.hands_per_match + 1):
            dealer_index = (hand_number - 1) % len(names)
            self._run_hand(match_id, hand_number, rng, lineup, dealer_index, scores, memories, trackers)
            hands_played = hand_number
            self._print_progress(match_id, hand_number, self.config.hands_per_match, names)

        winner_name = max(names, key=lambda name: scores[name])
        if len({scores[name] for name in names}) == 1:
            winner_name = "Draw"
        self.logger.write_match({"run_id": self.run_id, "match_id": match_id, "table_size": len(names), "players": "|".join(names), "seed": match_seed, "total_hands": hands_played, "starting_stack": self.config.starting_stack, "small_blind": self.config.small_blind, "big_blind": self.config.big_blind, "final_stacks": json.dumps(scores, sort_keys=True), "winner": winner_name})
        self.completed_matches += 1
        self._print_progress(match_id, self.config.hands_per_match, self.config.hands_per_match, names, winner_name, done=True)

    def _run_hand(self, match_id: str, hand_id: int, rng: random.Random, lineup: List[BotConfig], dealer_index: int, scores: Dict[str, int], memories: Dict[str, List[HandMemory]], trackers: Dict[str, BotTracker]) -> None:
        names = [bot.name for bot in lineup]
        stacks = {name: self.config.starting_stack for name in names}
        deck = build_deck(rng)
        hole_cards = {name: [deck.pop(), deck.pop()] for name in names}
        board: List[Tuple[int, str]] = []
        current_bets = {name: 0 for name in names}
        folded = {name: False for name in names}
        round_history: List[ActionRecord] = []
        pot = 0
        decision_id = 0
        end_round = "PreFlop"
        showdown = False

        for blind_amount, blind_name in self._blind_positions(names, dealer_index):
            paid = min(blind_amount, stacks[blind_name])
            stacks[blind_name] -= paid
            current_bets[blind_name] += paid
            pot += paid
        highest_bet = max(current_bets.values())

        hand_over, pot, highest_bet, decision_id = self._play_betting_round("PreFlop", self._preflop_order(names, dealer_index), lineup, stacks, hole_cards, board, current_bets, folded, round_history, memories, trackers, pot, highest_bet, match_id, hand_id, decision_id)

        for stage, draw_count in (("Flop", 3), ("Turn", 1), ("River", 1)):
            if hand_over or self._remaining_players(names, folded) <= 1:
                break
            board.extend(deck.pop() for _ in range(draw_count))
            current_bets = {name: 0 for name in names}
            highest_bet = 0
            end_round = stage
            if self._all_eligible_all_in(names, folded, stacks):
                continue
            hand_over, pot, highest_bet, decision_id = self._play_betting_round(stage, self._postflop_order(names, dealer_index), lineup, stacks, hole_cards, board, current_bets, folded, round_history, memories, trackers, pot, highest_bet, match_id, hand_id, decision_id)

        if self._remaining_players(names, folded) == 1:
            winners = [next(name for name in names if not folded[name])]
        else:
            showdown = True
            while len(board) < 5:
                board.append(deck.pop())
            showdown_scores = {name: best_hand([*hole_cards[name], *board]) for name in names if not folded[name]}
            best_score = max(showdown_scores.values())
            winners = [name for name, score in showdown_scores.items() if score == best_score]
            for winner_name in winners:
                trackers[winner_name].showdown_wins += 1
            end_round = "Showdown"

        share = pot // len(winners)
        remainder = pot % len(winners)
        for index, winner_name in enumerate(winners):
            stacks[winner_name] += share + (1 if index < remainder else 0)
        hand_profit = {name: stacks[name] - self.config.starting_stack for name in names}
        for name in names:
            scores[name] += hand_profit[name]
        for name in names:
            trackers[name].hands_observed += 1
            memories[name].append(HandMemory(roundNumber=hand_id, winnerName=", ".join(winners), finalPot=pot, summary=f"{', '.join(winners)} won {pot} chips in {end_round}."))

        self.logger.write_hand({"run_id": self.run_id, "match_id": match_id, "table_size": len(names), "hand_id": hand_id, "dealer": names[dealer_index], "winner": ", ".join(winners), "win_amount": pot, "pot_size": pot, "end_round": end_round, "showdown": showdown, "stacks_after": json.dumps(hand_profit, sort_keys=True)})

    def _play_betting_round(self, stage: str, acting_order: List[str], lineup: List[BotConfig], stacks: Dict[str, int], hole_cards: Dict[str, List[Tuple[int, str]]], board: List[Tuple[int, str]], current_bets: Dict[str, int], folded: Dict[str, bool], round_history: List[ActionRecord], memories: Dict[str, List[HandMemory]], trackers: Dict[str, BotTracker], pot: int, highest_bet: int, match_id: str, hand_id: int, decision_id: int) -> Tuple[bool, int, int, int]:
        acted: set[str] = set()
        while True:
            if self._remaining_players(acting_order, folded) <= 1:
                return True, pot, highest_bet, decision_id
            pending = [name for name in acting_order if not folded[name] and stacks[name] > 0 and (name not in acted or current_bets[name] != highest_bet)]
            if not pending:
                return False, pot, highest_bet, decision_id
            player_name = pending[0]
            hand_over, pot, highest_bet, decision_id, did_raise = self._single_action(stage, player_name, lineup, stacks, hole_cards, board, current_bets, folded, round_history, memories, trackers, pot, highest_bet, match_id, hand_id, decision_id)
            acted.add(player_name)
            if did_raise:
                acted = {player_name}
            if hand_over:
                return True, pot, highest_bet, decision_id

    def _single_action(self, stage: str, player_name: str, lineup: List[BotConfig], stacks: Dict[str, int], hole_cards: Dict[str, List[Tuple[int, str]]], board: List[Tuple[int, str]], current_bets: Dict[str, int], folded: Dict[str, bool], round_history: List[ActionRecord], memories: Dict[str, List[HandMemory]], trackers: Dict[str, BotTracker], pot: int, highest_bet: int, match_id: str, hand_id: int, decision_id: int) -> Tuple[bool, int, int, int, bool]:
        bot = next(item for item in lineup if item.name == player_name)
        opponent_names = [item.name for item in lineup if item.name != player_name]
        to_call = max(0, highest_bet - current_bets[player_name])
        can_raise = stacks[player_name] > to_call
        min_raise = self.config.big_blind if can_raise else 0
        max_raise = self._max_raise_extra(stage, pot, stacks[player_name], to_call) if can_raise else 0
        if min_raise > max_raise:
            can_raise = False
            min_raise = 0
            max_raise = 0
        request = DecisionRequest(
            phase=stage,
            potSize=pot,
            highestBet=highest_bet,
            callAmount=to_call,
            minRaiseAmount=min_raise,
            maxRaiseAmount=max_raise,
            canFold=True,
            canCheck=to_call == 0,
            canCall=to_call > 0 and stacks[player_name] > 0,
            canRaise=can_raise,
            me=PlayerState(name=player_name, stack=stacks[player_name], currentBet=current_bets[player_name], isFolded=False, isAllIn=stacks[player_name] == 0, hand=[card_to_model(card) for card in hole_cards[player_name]]),
            opponents=[PlayerState(name=name, stack=stacks[name], currentBet=current_bets[name], isFolded=folded[name], isAllIn=stacks[name] == 0, hand=[]) for name in opponent_names],
            communityCards=[card_to_model(card) for card in board],
            currentRoundHistory=list(round_history),
            memory=memories[player_name][-bot.history_window:] if bot.history_window > 0 else [],
            opponentStats=[trackers[name].build_stats(name) for name in opponent_names] if bot.use_opponent_stats else [],
            botProfile=BotProfile(name=bot.name, styleNotes=bot.style_notes, historyWindow=bot.history_window, useOpponentStats=bot.use_opponent_stats, modelName=bot.model_name),
        )
        started = time.perf_counter()
        decision = self.policies[player_name].decide(request)
        elapsed_ms = round((time.perf_counter() - started) * 1000.0, 3)
        action = decision.action.capitalize()
        amount = max(0, decision.amount)
        did_raise = False

        if action == "Fold":
            folded[player_name] = True
        elif action == "Call":
            paid = min(to_call, stacks[player_name])
            stacks[player_name] -= paid
            current_bets[player_name] += paid
            pot += paid
        elif action == "Raise" and can_raise:
            raise_amount = max(min_raise, min(amount, max_raise))
            paid = min(to_call + raise_amount, stacks[player_name])
            stacks[player_name] -= paid
            current_bets[player_name] += paid
            highest_bet = current_bets[player_name]
            pot += paid
            amount = raise_amount
            did_raise = True
        else:
            fallback_action = "Check" if to_call == 0 else "Call"
            action = fallback_action
            if fallback_action == "Call":
                paid = min(to_call, stacks[player_name])
                stacks[player_name] -= paid
                current_bets[player_name] += paid
                pot += paid
            amount = 0

        trackers[player_name].record_action(action)
        round_history.append(ActionRecord(phase=stage, playerName=player_name, action=action, amount=amount))
        decision_id += 1
        self.logger.write_decision({"run_id": self.run_id, "match_id": match_id, "table_size": len(lineup), "hand_id": hand_id, "decision_id": decision_id, "player": player_name, "profile": bot.name, "stage": stage, "pot_size": pot, "to_call": to_call, "player_chips": stacks[player_name], "chosen_action": action, "bet_amount": amount, "response_time_ms": elapsed_ms, "history_window_size": bot.history_window, "used_opponent_stats": bot.use_opponent_stats, "used_fallback": decision.usedFallback, "reason": decision.reason})
        return self._remaining_players([item.name for item in lineup], folded) <= 1, pot, highest_bet, decision_id, did_raise

    def _max_raise_extra(self, stage: str, stack: int, stack_remaining: int, to_call: int) -> int:
        stage_name = stage.lower()
        effective_stack = max(0, stack_remaining - to_call)
        if effective_stack <= 0:
            return 0

        if stage_name == "preflop":
            short_stack_threshold = 10 * self.config.big_blind
            if stack_remaining <= short_stack_threshold:
                return effective_stack
            return min(effective_stack, 3 * self.config.big_blind)

        pot_based_cap = int(max(self.config.big_blind, round(stack * 0.75)))
        stack_based_cap = int(max(self.config.big_blind, round(stack_remaining * 0.5)))
        short_stack_threshold = 8 * self.config.big_blind
        if stack_remaining <= short_stack_threshold:
            return effective_stack
        return min(effective_stack, pot_based_cap, stack_based_cap)

    def _blind_positions(self, names: List[str], dealer_index: int) -> List[Tuple[int, str]]:
        if len(names) == 2:
            return [(self.config.small_blind, names[dealer_index]), (self.config.big_blind, names[(dealer_index + 1) % len(names)])]
        return [(self.config.small_blind, names[(dealer_index + 1) % len(names)]), (self.config.big_blind, names[(dealer_index + 2) % len(names)])]

    def _preflop_order(self, names: List[str], dealer_index: int) -> List[str]:
        if len(names) == 2:
            return [names[dealer_index], names[(dealer_index + 1) % len(names)]]
        start = (dealer_index + 3) % len(names)
        return [names[(start + offset) % len(names)] for offset in range(len(names))]

    def _postflop_order(self, names: List[str], dealer_index: int) -> List[str]:
        start = (dealer_index + 1) % len(names)
        return [names[(start + offset) % len(names)] for offset in range(len(names))]

    def _remaining_players(self, names: Sequence[str], folded: Dict[str, bool]) -> int:
        return sum(1 for name in names if not folded[name])

    def _all_eligible_all_in(self, names: Sequence[str], folded: Dict[str, bool], stacks: Dict[str, int]) -> bool:
        eligible = [name for name in names if not folded[name]]
        return all(stacks[name] == 0 for name in eligible)

    def _print_progress(self, match_id: str, hand_number: int, total_hands: int, names: List[str], winner: str | None = None, done: bool = False) -> None:
        hand_progress = hand_number / max(1, total_hands)
        match_progress = (self.completed_matches + hand_progress) / max(1, self.total_matches)
        bar_width = 24
        filled = int(bar_width * match_progress)
        bar = "#" * filled + "-" * (bar_width - filled)
        lineup = " vs ".join(names)
        suffix = f" winner={winner}" if done and winner else ""
        line = f"\r[{bar}] {match_progress * 100:6.2f}% | match {self.completed_matches + (0 if done else 1)}/{self.total_matches} | hand {hand_number}/{total_hands} | {lineup}{suffix}"
        pad = max(0, self._last_progress_len - len(line))
        sys.stdout.write(line + (" " * pad))
        sys.stdout.flush()
        self._last_progress_len = len(line)
