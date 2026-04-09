import argparse
from pathlib import Path

from poker_agent.config import settings
from poker_agent.sim_support import BotConfig, SimulationConfig
from poker_agent.sim_tables import PokerSimulation


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run poker bot round-robin simulations.")
    parser.add_argument("--hands", type=int, default=100, help="Hands per match.")
    parser.add_argument("--matches", type=int, default=3, help="Matches per pairing.")
    parser.add_argument("--stack", type=int, default=1000, help="Starting stack per bot.")
    parser.add_argument("--seed", type=int, default=7, help="Base RNG seed.")
    parser.add_argument("--output", type=Path, default=Path("runs"), help="Output directory root.")
    parser.add_argument("--model", default=settings.model_name, help="LM Studio model name for weak/main bots.")
    parser.add_argument("--baseline-only", action="store_true", help="Run a smoke test with baseline bots only.")
    parser.add_argument("--table-sizes", type=int, nargs="+", default=[2, 3], help="Table sizes to simulate, e.g. --table-sizes 2 3.")
    return parser.parse_args()


def build_default_bots(model_name: str, baseline_only: bool, table_sizes: list[int]) -> list[BotConfig]:
    if baseline_only:
        bots = [
            BotConfig(
                name="baseline_a",
                policy="baseline",
                history_window=0,
                use_opponent_stats=False,
                style_notes="Smoke-test baseline bot A.",
            ),
            BotConfig(
                name="baseline_b",
                policy="baseline",
                history_window=0,
                use_opponent_stats=False,
                style_notes="Smoke-test baseline bot B.",
            ),
        ]
        if any(size >= 3 for size in table_sizes):
            bots.append(
                BotConfig(
                    name="baseline_c",
                    policy="baseline",
                    history_window=0,
                    use_opponent_stats=False,
                    style_notes="Smoke-test baseline bot C.",
                )
            )
        return bots

    return [
        BotConfig(
            name="baseline",
            policy="baseline",
            history_window=0,
            use_opponent_stats=False,
            style_notes="Rule-based baseline with no learning.",
        ),
        BotConfig(
            name="weak_ai",
            policy="llm",
            history_window=3,
            use_opponent_stats=False,
            style_notes="Short memory poker bot. Focus on current street and avoid risky bluffs.",
            model_name=model_name,
        ),
        BotConfig(
            name="main_ai",
            policy="llm",
            history_window=20,
            use_opponent_stats=True,
            style_notes="Longer-memory poker bot. Use recent trends to exploit folds and weak calls.",
            model_name=model_name,
        ),
    ]


def main() -> None:
    args = parse_args()
    simulation = PokerSimulation(
        bots=build_default_bots(args.model, args.baseline_only, args.table_sizes),
        config=SimulationConfig(
            starting_stack=args.stack,
            hands_per_match=args.hands,
            matches_per_pairing=args.matches,
            seed=args.seed,
            table_sizes=tuple(args.table_sizes),
        ),
        output_root=args.output,
    )
    output_path = simulation.run_tables()
    print(f"Simulation finished. Logs saved to: {output_path}")


if __name__ == "__main__":
    main()
