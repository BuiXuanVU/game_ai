import argparse
import json
from pathlib import Path

import matplotlib.pyplot as plt
import pandas as pd


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


def save_match_wins(matches: pd.DataFrame, output_path: Path, title_suffix: str) -> None:
    fig, ax = plt.subplots(figsize=(8, 5))
    counts = (
        matches.groupby(["players", "winner"])
        .size()
        .reset_index(name="matches_won")
        .pivot(index="players", columns="winner", values="matches_won")
        .fillna(0)
    )
    counts.plot(kind="bar", stacked=True, ax=ax, colormap="tab20")
    ax.set_title(f"Match Wins by Pairing\n{title_suffix}")
    ax.set_xlabel("Pairing")
    ax.set_ylabel("Matches won")
    ax.tick_params(axis="x", rotation=15)
    ax.legend(title="Winner", fontsize=8, title_fontsize=9)
    fig.tight_layout()
    fig.savefig(output_path, dpi=180, bbox_inches="tight")
    plt.close(fig)


def save_net_chip_ev(matches: pd.DataFrame, output_path: Path, title_suffix: str) -> None:
    fig, ax = plt.subplots(figsize=(8, 5))
    net_chips = expand_match_net_chips(matches)
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
    ax.set_title(f"Average Net Chips per Match\n{title_suffix}")
    ax.set_xlabel("Pairing / Player")
    ax.set_ylabel("Avg net chips")
    ax.tick_params(axis="x", rotation=20)
    fig.tight_layout()
    fig.savefig(output_path, dpi=180, bbox_inches="tight")
    plt.close(fig)


def save_action_mix(decisions: pd.DataFrame, output_path: Path, title_suffix: str) -> None:
    fig, ax = plt.subplots(figsize=(8, 5))
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
    ax.set_title(f"Decision Mix by Bot\n{title_suffix}")
    ax.set_xlabel("Bot")
    ax.set_ylabel("Action share (%)")
    ax.tick_params(axis="x", rotation=0)
    ax.legend(title="Action", fontsize=8, title_fontsize=9)
    fig.tight_layout()
    fig.savefig(output_path, dpi=180, bbox_inches="tight")
    plt.close(fig)


def save_hand_endings(hands: pd.DataFrame, output_path: Path, title_suffix: str) -> None:
    fig, ax = plt.subplots(figsize=(8, 5))
    ending_counts = hands["end_round"].value_counts().reindex(["Flop", "Turn", "River", "Showdown"], fill_value=0)
    ax.bar(ending_counts.index, ending_counts.values, color=["#42a5f5", "#26a69a", "#ffa726", "#ab47bc"])
    ax.set_title(f"Where Hands End\n{title_suffix}")
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
    fig.tight_layout()
    fig.savefig(output_path, dpi=180, bbox_inches="tight")
    plt.close(fig)


def main() -> None:
    parser = argparse.ArgumentParser(description="Export individual charts for a poker simulation run.")
    parser.add_argument("run_dir", type=Path, help="Path to the run directory containing CSV outputs.")
    parser.add_argument("--output-dir", type=Path, default=None, help="Directory to save chart PNGs.")
    args = parser.parse_args()

    run_dir = args.run_dir.resolve()
    output_dir = (args.output_dir or (run_dir / "charts")).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    matches, hands, decisions = load_data(run_dir)
    title_suffix = run_dir.name

    plt.style.use("ggplot")
    save_match_wins(matches, output_dir / "match_wins.png", title_suffix)
    save_net_chip_ev(matches, output_dir / "net_chip_ev.png", title_suffix)
    save_action_mix(decisions, output_dir / "decision_mix.png", title_suffix)
    save_hand_endings(hands, output_dir / "hand_endings.png", title_suffix)

    print(f"Saved charts to: {output_dir}")


if __name__ == "__main__":
    main()
