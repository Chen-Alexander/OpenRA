#!/usr/bin/env python3
"""Compare English Fluent sources with a translation overlay.

Usage:
    python tools/solar/check-fluent.py --lang zh-Hans
    python tools/solar/check-fluent.py --lang zh-Hans --strict
"""

import argparse
import re
import sys
from pathlib import Path

MESSAGE_RE = re.compile(r"^([A-Za-z][A-Za-z0-9_.-]*)\s*=")
PLACEHOLDER_RE = re.compile(r"\{\s*\$([A-Za-z0-9_-]+)")


def parse_messages(path: Path) -> dict[str, set[str]]:
    messages: dict[str, set[str]] = {}
    current = None
    for line in path.read_text(encoding="utf-8").splitlines():
        match = MESSAGE_RE.match(line)
        if match:
            current = match.group(1)
            messages[current] = set(PLACEHOLDER_RE.findall(line))
            continue

        if current is not None and line[:1] in (" ", "\t"):
            messages[current].update(PLACEHOLDER_RE.findall(line))
        elif line.strip():
            current = None

    return messages


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--lang", default="zh-Hans")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--strict", action="store_true", help="Fail when keys or files are missing")
    args = parser.parse_args()

    root: Path = args.root
    lang: str = args.lang

    english_files = sorted(
        p for p in root.glob("mods/*/fluent/*.ftl") if p.parent.name != lang
    )
    if not english_files:
        print(f"No English Fluent files found under {root}")
        return 1

    missing_files = 0
    missing_keys = 0
    extra_keys = 0
    placeholder_mismatches = 0

    for en_path in english_files:
        rel = en_path.relative_to(root)
        lang_path = en_path.parent / lang / en_path.name

        if not lang_path.exists():
            missing_files += 1
            print(f"MISSING FILE  {rel}  ->  {lang_path.relative_to(root)}")
            continue

        en = parse_messages(en_path)
        tr = parse_messages(lang_path)

        for key, placeholders in en.items():
            if key not in tr:
                missing_keys += 1
                print(f"MISSING KEY   {rel}: {key}")
                continue
            if placeholders != tr[key]:
                placeholder_mismatches += 1
                print(
                    f"PLACEHOLDER   {rel}: {key}  "
                    f"en={sorted(placeholders)} {lang}={sorted(tr[key])}"
                )

        for key in tr.keys() - en.keys():
            extra_keys += 1
            print(f"EXTRA KEY     {lang_path.relative_to(root)}: {key}")

    print()
    print(
        f"Checked {len(english_files)} files for '{lang}': "
        f"{missing_files} missing files, {missing_keys} missing keys, "
        f"{extra_keys} extra keys, {placeholder_mismatches} placeholder mismatches."
    )

    failed = placeholder_mismatches > 0
    if args.strict:
        failed = failed or missing_files > 0 or missing_keys > 0 or extra_keys > 0

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
