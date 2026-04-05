#!/usr/bin/env python3
"""Append a dated progress bullet into docs/Sprint-Remaining-Tasks-2026-04-05.md.

Usage:
  python scripts/update-sprint-remaining.py --task-id provider:foo --note "Implemented ..."
"""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
from pathlib import Path

TRACKER_PATH = Path("docs/Sprint-Remaining-Tasks-2026-04-05.md")
MARKER = "### Текущий прогресс"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Append sprint progress note")
    parser.add_argument("--task-id", required=True, help="Task identifier")
    parser.add_argument("--note", required=True, help="One-line completion/progress note")
    parser.add_argument(
        "--status",
        choices=["done", "in-progress"],
        default="done",
        help="Task status marker",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    if not TRACKER_PATH.exists():
        raise FileNotFoundError(f"Tracker file not found: {TRACKER_PATH}")

    content = TRACKER_PATH.read_text(encoding="utf-8")
    if MARKER not in content:
        raise ValueError(f"Expected marker '{MARKER}' not found in {TRACKER_PATH}")

    utc_now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    checkbox = "[x]" if args.status == "done" else "[ ]"
    new_line = f"- {checkbox} `{args.task_id}` — {args.note} ({utc_now})"

    insert_pos = content.index(MARKER) + len(MARKER)
    updated = f"{content[:insert_pos]}\n\n{new_line}{content[insert_pos:]}"
    TRACKER_PATH.write_text(updated, encoding="utf-8")

    print(f"Updated {TRACKER_PATH} with: {new_line}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
