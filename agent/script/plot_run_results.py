import argparse
import json
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Plot summary charts for a poker simulation run.")
    parser.add_argument("run_dir", type=Path, help="Path to the run directory containing CSV outputs.")
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Output image path. Defaults to <run_dir>/trend_report.png",
    )
    return parser.parse_args()


def load_data(run_dir: Path) -> tuple[pd.DataFrame, pd.DataFrame, pd.DataFrame]:
    matches = pd.read_csv(run_dir / "matches.csv")
    hands = pd.read_csv(run_dir / "hands.csv")
    decisions = pd.read_csv(run_dir / "decisions.csv")
    return matches, hands, decisions


def expand_match_net_chips(matches: pd.DataFrame) -> pd.DataFrame:
    rows: list[dict] = []
    for row in matches.itertuples(index=False):
        final_stacks = json.loads(row.final_stacks)
        for player, net_chips in final_stacks.items():
            rows.append(
                {
                    "match_id": row.match_id,
                    "pairing": row.players,
                    "winner": row.winner,
                    "player": player,
                    "net_chips": int(net_chips),
                }
            )
    return pd.DataFrame(rows)


def plot_match_wins(ax: plt.Axes, matches: pd.DataFrame) -> None:
    counts = (
        matches.groupby(["players", "winner"])
        .size()
        .reset_index(name="matches_won")
        .pivot(index="players", columns="winner", values="matches_won")
        .fillna(0)
    )
    counts.plot(kind="bar", stacked=True, ax=ax, colormap="tab20")
    ax.set_title("Match Wins by Pairing")
    ax.set_xlabel("Pairing")
    ax.set_ylabel("Matches won")
    ax.tick_params(axis="x", rotation=15)
    ax.legend(title="Winner", fontsize=8, title_fontsize=9)


def plot_net_chip_ev(ax: plt.Axes, net_chips: pd.DataFrame) -> None:
    summary = (
        net_chips.groupby(["pairing", "player"])["net_chips"]
        .mean()
        .reset_index()
        .sort_values(["pairing", "net_chips"], ascending=[True, False])
    )
    labels = [f"{pairing}\n{player}" for pairing, player in zip(summary["pairing"], summary["player"])]
    colors = ["#2e7d32" if value >= 0 else "#c62828" for value in summary["net_chips"]]
    ax.bar(labels, summary["net_chips"], color=colors)
    ax.axhline(0, color="#333333", linewidth=1)
    ax.set_title("Average Net Chips per Match")
    ax.set_xlabel("Pairing / Player")
    ax.set_ylabel("Avg net chips")
    ax.tick_params(axis="x", rotation=20)


def plot_action_mix(ax: plt.Axes, decisions: pd.DataFrame) -> None:
    action_mix = (
        decisions.groupby(["player", "chosen_action"])
        .size()
        .reset_index(name="count")
        .pivot(index="player", columns="chosen_action", values="count")
        .fillna(0)
    )
    action_mix = action_mix.div(action_mix.sum(axis=1), axis=0) * 100.0
    action_mix = action_mix.reindex(columns=["Fold", "Check", "Call", "Raise"], fill_value=0)
    action_mix.plot(kind="bar", stacked=True, ax=ax, color=["#8e8e8e", "#4fc3f7", "#ffb300", "#ef5350"])
    ax.set_title("Decision Mix by Bot")
    ax.set_xlabel("Bot")
    ax.set_ylabel("Action share (%)")
    ax.tick_params(axis="x", rotation=0)
    ax.legend(title="Action", fontsize=8, title_fontsize=9)


def plot_hand_endings(ax: plt.Axes, hands: pd.DataFrame) -> None:
    ending_counts = hands["end_round"].value_counts().reindex(["Flop", "Turn", "River", "Showdown"], fill_value=0)
    ax.bar(ending_counts.index, ending_counts.values, color=["#42a5f5", "#26a69a", "#ffa726", "#ab47bc"])
    ax.set_title("Where Hands End")
    ax.set_xlabel("End round")
    ax.set_ylabel("Hands")

    total_hands = int(len(hands))
    showdown_rate = 100.0 * float((hands["showdown"] == True).sum()) / max(total_hands, 1)
    ax.text(
        0.98,
        0.95,
        f"Total hands: {total_hands}\nShowdown rate: {showdown_rate:.1f}%",
        transform=ax.transAxes,
        ha="right",
        va="top",
        fontsize=9,
        bbox={"boxstyle": "round", "facecolor": "white", "alpha": 0.8},
    )


def main() -> None:
    args = parse_args()
    run_dir = args.run_dir.resolve()
    output_path = (args.output or (run_dir / "trend_report.png")).resolve()

    matches, hands, decisions = load_data(run_dir)
    net_chips = expand_match_net_chips(matches)

    plt.style.use("ggplot")
    fig, axes = plt.subplots(2, 2, figsize=(16, 10))
    fig.suptitle(f"Poker Run Trend Report\n{run_dir.name}", fontsize=16)

    plot_match_wins(axes[0, 0], matches)
    plot_net_chip_ev(axes[0, 1], net_chips)
    plot_action_mix(axes[1, 0], decisions)
    plot_hand_endings(axes[1, 1], hands)

    plt.tight_layout(rect=(0, 0, 1, 0.96))
    fig.savefig(output_path, dpi=180, bbox_inches="tight")
    print(f"Saved chart to: {output_path}")


if __name__ == "__main__":
    main()
