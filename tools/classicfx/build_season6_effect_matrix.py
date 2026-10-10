#!/usr/bin/env python3
"""ClassicFX source-driven native FX call inventory (static candidates, not S6 parity)."""
import argparse
import bisect
import csv
import json
import re
import subprocess
from pathlib import Path

REFERENCE = "21728b1e5b03e0763b38ef9e23f79645e0df7ad2"
FILES = (
    "src/source/Engine/Object/ZzzCharacter.cpp",
    "src/source/Render/Effects/ZzzEffectMagicSkill.cpp",
    "src/source/Render/Effects/ZzzEffect.cpp",
    "src/source/Render/Effects/Behaviors/MoveHandlers.cpp",
)
CALL = re.compile(r"\b(Create(?:Effect|Particle|Joint|Sprite|Blur|ObjectBlur)(?:FpsChecked)?)\s*\(\s*([A-Za-z_]\w*(?:\s*\+\s*\d+)?)")
CASE = re.compile(r"\bcase\s+((?:AT_SKILL|MODEL|BITMAP)_[A-Za-z0-9_]+)\s*:")
MOVE = re.compile(r"\b(?:bool|void)\s+(Move_MODEL_[A-Z0-9_]+)\s*\(")
ATTR = re.compile(r"\[\s*SkillVisualEffect\s*\(\s*(\d+)\s*\)\s*\]")
ENUM = re.compile(r"^\s*([A-Za-z]\w*)\s*=\s*\d+\s*,?\s*$", re.M)
FIELDS = ["origin", "source", "line", "primitive", "resource", "context",
          "context_kind", "scope", "registered_effect"]

def scan(content, path, index, registered):
    newlines = [m.start() for m in re.finditer("\n", content)]
    candidates = MOVE if index == 3 else CASE
    labels = [(m.start(), m.group(1)) for m in candidates.finditer(content)]
    label_offsets = [x[0] for x in labels]
    rows = []
    for m in CALL.finditer(content):
        primitive, resource = m.groups()
        resource = re.sub(r"\s+", "", resource)
        token = resource.split("+")[0]
        if not token.startswith(("MODEL_", "BITMAP_")):
            continue
        loc = bisect.bisect_right(label_offsets, m.start()) - 1
        name = labels[loc][1] if loc >= 0 and (index == 3 or m.start() - labels[loc][0] <= 3000) else ""
        kind = "move_handler" if index == 3 else "skill_case" if name.startswith("AT_SKILL_") else "native_case" if name else "unknown"
        origin = "cast_source" if index < 2 else "effect_source" if index == 2 else "move_handler"
        rows.append(dict(
            origin=origin, source=path,
            line=bisect.bisect_left(newlines, m.start()) + 1,
            primitive=primitive, resource=resource, context=name, context_kind=kind,
            scope="skill_candidate" if origin == "cast_source" and kind == "skill_case" else "secondary_candidate" if index > 1 else "unscoped_candidate",
            registered_effect=registered.get(token, ""),
        ))
    return rows

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--main", required=True, type=Path, help="checkout of pinned MuMain SHA")
    p.add_argument("--client", required=True, type=Path, help="BroyalMU checkout")
    p.add_argument("--out", type=Path, default=Path("classicfx-audit-out"))
    p.add_argument("--allow-different-main-sha", action="store_true")
    a = p.parse_args()
    try:
        sha = subprocess.check_output(["git", "-C", str(a.main), "rev-parse", "HEAD"], text=True).strip()
    except (OSError, subprocess.CalledProcessError):
        p.error("MuMain directory must be a valid git checkout")
    if sha != REFERENCE and not a.allow_different_main_sha:
        p.error(f"MuMain HEAD is {sha}, required {REFERENCE}")
    enum_source = (a.client / "Client.Main/ClassicFX/Core/ClassicFxRuntime.Effects.cs").read_text(encoding="utf-8-sig")
    supported = set(ENUM.findall(enum_source))
    # Deliberately only explicit, audited correspondences: never infer
    # coverage for 464 catalog types from the presence of an enum.
    mapping = {
        "MODEL_ALICE_BUFFSKILL_EFFECT": "AliceBuffSkillEffect",
        "MODEL_ALICE_BUFFSKILL_EFFECT2": "AliceBuffSkillEffect2",
        "MODEL_WAVE": "Wave",
        "MODEL_SHOCKWAVE_GROUND01": "ShockWaveGround01",
        "MODEL_AIR_FORCE": "AirForce",
        "MODEL_SWORD_FORCE": "SwordForce",
        "MODEL_MAGIC1": "Magic1",
        "MODEL_MAGIC_CIRCLE1": "MagicCircle1",
        "MODEL_POISON": "Poison",
        "MODEL_DARKLORD_SKILL": "DarkLordSkill",
        "MODEL_SWELL_OF_MAGICPOWER": "SwellOfMagicPower",
    }
    registered = {n: mapped for n, mapped in mapping.items() if mapped in supported}
    rows = []
    for i, path in enumerate(FILES):
        f = a.main / path
        if not f.is_file():
            p.error(f"Main source missing: {f}")
        rows.extend(scan(f.read_text(encoding="utf-8-sig"), path, i, registered))
    handlers = []
    for f in sorted((a.client / "Client.Main/Objects/Effects/Skills").glob("*.cs")):
        for m in ATTR.finditer(f.read_text(encoding="utf-8-sig")):
            handlers.append({"id": int(m.group(1)), "handler": f.name})
    native = json.loads((a.main / "src/bin/Data/Effects/EffectTypes.json").read_text(encoding="utf-8-sig"))
    a.out.mkdir(parents=True, exist_ok=True)
    with (a.out / "season6_effect_matrix.csv").open("w", newline="", encoding="utf-8") as out:
        writer = csv.DictWriter(out, FIELDS)
        writer.writeheader()
        writer.writerows(rows)
    summary = {
        "source_main_sha": REFERENCE, "actual_main_sha": sha,
        "scope": "Static candidate call sites, NOT a verified S6 skill closure.",
        "native_sources": list(FILES), "client_registered_handlers": handlers,
        "native_catalog_size": len(native["types"]),
        "native_effect_catalog": native["types"], "classicfx_enum": sorted(supported),
        "counts": {"call_sites": len(rows), "skill_handlers": len(handlers)},
        "calls": rows,
    }
    (a.out / "season6_effect_audit.json").write_text(
        json.dumps(summary, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{len(rows)} static candidate call sites / {len(handlers)} registered visual handlers")
    print(a.out.resolve())

if __name__ == "__main__":
    main()
