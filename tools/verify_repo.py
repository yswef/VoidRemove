#!/usr/bin/env python3
"""
Offline repository consistency check.

    python tools/verify_repo.py

This is the part of CI that does not need the .NET SDK: it verifies that the solution,
the project files, the embedded catalogs, the central package versions, the generated
identity mirrors and the brand assets all agree with each other, so that a broken edit is
caught here instead of on a Windows build agent.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent

FAILURES: list[str] = []
NOTES: list[str] = []

EXCLUDED_DIRS = {".git", "bin", "obj", ".venv", "venv", "node_modules", "site", "__pycache__", ".vs", ".idea"}


def fail(message: str) -> None:
    FAILURES.append(message)
    print(f"  FAIL  {message}")


def ok(message: str) -> None:
    print(f"  ok    {message}")


def iter_files(*suffixes: str) -> list[Path]:
    found: list[Path] = []
    for path in REPO_ROOT.rglob("*"):
        if any(part in EXCLUDED_DIRS for part in path.parts):
            continue
        if path.is_file() and path.suffix in suffixes:
            found.append(path)
    return sorted(found)


# ---------------------------------------------------------------- solution / projects


def check_solution() -> None:
    sln = REPO_ROOT / "VoidRemove.sln"
    if not sln.exists():
        fail("VoidRemove.sln is missing")
        return

    projects = re.findall(r'=\s*"[^"]+",\s*"([^"]+\.(?:csproj|vcxproj))"', sln.read_text(encoding="utf-8"))
    missing = [p for p in projects if not (REPO_ROOT / p.replace("\\", "/")).exists()]
    for relative in missing:
        fail(f"solution references a missing project: {relative}")
    if not missing:
        ok(f"solution references {len(projects)} projects, all present")


def check_xml_wellformed() -> None:
    bad = 0
    for path in iter_files(".csproj", ".props", ".manifest", ".xaml"):
        try:
            ET.parse(path)
        except ET.ParseError as error:
            fail(f"not well-formed XML: {path.relative_to(REPO_ROOT)}: {error}")
            bad += 1
    if bad == 0:
        ok(f"{len(iter_files('.csproj', '.props', '.manifest', '.xaml'))} XML documents well-formed")


def check_json_parses() -> None:
    bad = 0
    count = 0
    for path in iter_files(".json"):
        if path.name in ("global.json",) or True:
            count += 1
            try:
                json.loads(path.read_text(encoding="utf-8"))
            except json.JSONDecodeError as error:
                fail(f"invalid JSON: {path.relative_to(REPO_ROOT)}: {error}")
                bad += 1
    if bad == 0:
        ok(f"{count} JSON documents parse")


# ---------------------------------------------------------------- central package management


def check_central_packages() -> None:
    props = REPO_ROOT / "Directory.Packages.props"
    tree = ET.parse(props)
    versions = {}
    for element in tree.iter():
        if element.tag.endswith("PackageVersion"):
            include = element.get("Include")
            version = element.get("Version")
            if include and version:
                versions[include] = version

    referenced = set()
    for csproj in iter_files(".csproj"):
        tree = ET.parse(csproj)
        for element in tree.iter():
            if element.tag.endswith("PackageReference"):
                name = element.get("Include")
                if name is None:
                    continue
                referenced.add(name)
                if element.get("Version") is not None:
                    fail(f"{csproj.name}: PackageReference {name} must not carry a Version (central management)")
                if name not in versions:
                    fail(f"{csproj.name}: PackageReference {name} has no PackageVersion in Directory.Packages.props")

    for unused in sorted(set(versions) - referenced):
        NOTES.append(f"PackageVersion '{unused}' is not referenced by any project yet")

    ok(f"{len(referenced)} referenced packages all pinned centrally ({len(versions)} pinned total)")


# ---------------------------------------------------------------- localization


def check_localization() -> None:
    en = json.loads((REPO_ROOT / "src/VoidRemove.Shared/Resources/strings.en.json").read_text(encoding="utf-8"))
    ar = json.loads((REPO_ROOT / "src/VoidRemove.Shared/Resources/strings.ar.json").read_text(encoding="utf-8"))

    if set(en) != set(ar):
        only_en = set(en) - set(ar)
        only_ar = set(ar) - set(en)
        fail(f"en/ar drift: only in en={sorted(only_en)[:6]} only in ar={sorted(only_ar)[:6]}")
    else:
        ok(f"en and ar catalogs agree on {len(en)} keys")

    keys_cs = (REPO_ROOT / "src/VoidRemove.Shared/Localization/LocalizationKeys.cs").read_text(encoding="utf-8")
    constants = set(re.findall(r'public const string \w+ = "([^"]+)";', keys_cs))

    if constants != set(en):
        fail(f"LocalizationKeys drift: missing constants={sorted(set(en) - constants)[:6]} extra={sorted(constants - set(en))[:6]}")
    else:
        ok(f"LocalizationKeys covers all {len(en)} catalog keys")

    for value in list(en.values()) + list(ar.values()):
        if not value.strip():
            fail("an empty translation exists")


# ---------------------------------------------------------------- no unfinished markers


def check_no_todo_markers() -> None:
    pattern = re.compile(r"\b(TODO|FIXME|XXX|STUB|PLACEHOLDER|NOT IMPLEMENTED|اكمل|ناقص)\b", re.IGNORECASE)
    hits = 0
    for path in iter_files(".cs", ".xaml", ".py", ".iss", ".md"):
        if path.name == "verify_repo.py":
            continue
        for number, line in enumerate(path.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
            if pattern.search(line):
                fail(f"unfinished marker at {path.relative_to(REPO_ROOT)}:{number}: {line.strip()[:60]}")
                hits += 1
    if hits == 0:
        ok("no TODO/FIXME/placeholder markers anywhere")


# ---------------------------------------------------------------- C# brace balance


def code_only(text: str) -> str:
    """
    Returns only the code characters of a C# file, dropping comments, string contents,
    verbatim strings and char literals. A small state machine, because regexes keep
    mishandling verbatim literals such as <c>@"Software\\"</c>.
    """
    out: list[str] = []
    i = 0
    n = len(text)
    state = "code"

    while i < n:
        ch = text[i]
        nxt = text[i + 1] if i + 1 < n else ""

        if state == "code":
            if ch == "/" and nxt == "/":
                state = "line"
                i += 2
                continue
            if ch == "/" and nxt == "*":
                state = "block"
                i += 2
                continue
            if ch == '"':
                state = "verbatim" if i > 0 and text[i - 1] == "@" else "string"
                i += 1
                continue
            if ch == "'":
                state = "char"
                i += 1
                continue
            if ch in "{}()[]":
                out.append(ch)
            i += 1
        elif state == "line":
            if ch == "\n":
                state = "code"
                out.append(ch)
            i += 1
        elif state == "block":
            if ch == "*" and nxt == "/":
                state = "code"
                i += 2
                continue
            i += 1
        elif state == "string":
            if ch == "\\" :
                i += 2
                continue
            if ch == '"':
                state = "code"
            i += 1
        elif state == "verbatim":
            if ch == '"' and nxt == '"':
                i += 2
                continue
            if ch == '"':
                state = "code"
            i += 1
        elif state == "char":
            if ch == "\\":
                i += 2
                continue
            if ch == "'":
                state = "code"
            i += 1

    return "".join(out)


def check_braces() -> None:
    bad = 0
    for path in iter_files(".cs"):
        cleaned = code_only(path.read_text(encoding="utf-8", errors="replace"))
        if cleaned.count("{") != cleaned.count("}"):
            fail(f"unbalanced braces in {path.relative_to(REPO_ROOT)}")
            bad += 1
        if cleaned.count("(") != cleaned.count(")"):
            fail(f"unbalanced parentheses in {path.relative_to(REPO_ROOT)}")
            bad += 1
        if cleaned.count("[") != cleaned.count("]"):
            fail(f"unbalanced brackets in {path.relative_to(REPO_ROOT)}")
            bad += 1
    if bad == 0:
        ok(f"{len(iter_files('.cs'))} C# files have balanced braces, parentheses and brackets")


# ---------------------------------------------------------------- assets


def check_assets() -> None:
    required = [
        REPO_ROOT / "assets" / "logo.svg",
        REPO_ROOT / "assets" / "logo-mono.svg",
        REPO_ROOT / "assets" / "icon.ico",
    ]
    required += [REPO_ROOT / "assets" / "png" / f"icon-{size}.png" for size in (16, 32, 48, 64, 128, 256, 512)]
    missing = [str(p.relative_to(REPO_ROOT)) for p in required if not p.exists()]
    if missing:
        fail(f"missing brand assets: {missing}")
    else:
        ok(f"{len(required)} brand assets present (run tools/build_icons.py to regenerate)")


# ---------------------------------------------------------------- identity mirrors


def check_sync() -> None:
    try:
        result = subprocess.run(
            [sys.executable, str(REPO_ROOT / "tools" / "sync_appinfo.py"), "--check"],
            capture_output=True,
            text=True,
            cwd=REPO_ROOT,
        )
        if result.returncode != 0:
            fail(f"installer/docs identity mirrors are stale:\n{result.stdout}")
        else:
            ok(result.stdout.strip() or "identity mirrors up to date")
    except FileNotFoundError as error:
        fail(f"could not run sync_appinfo.py: {error}")


def main() -> int:
    print("VoidRemove repository verification\n")

    check_solution()
    check_xml_wellformed()
    check_json_parses()
    check_central_packages()
    check_localization()
    check_no_todo_markers()
    check_braces()
    check_assets()
    check_sync()

    print()
    for note in NOTES:
        print(f"  note  {note}")

    if FAILURES:
        print(f"\n{len(FAILURES)} problem(s) found.")
        return 1

    print("All repository checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
