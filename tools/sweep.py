#!/usr/bin/env python3
"""Build sweeps: play every spell combination of every class with Autoplay, then rank builds,
spells and items. Standard library only.

    tools/sweep.py run    OUT [--classes mage,fighter] [--runs 5] [--seed 1] [--shop smart] [-j 8] [-- extra autoplay flags]
    tools/sweep.py report OUT [--json results.json] [--item-window 4]

`run` plays C(8,3) = 56 builds per class (all spells auto-cast, smart movement), one Godot launch
per build with --runs seeds, and writes OUT/<class>/<spells>.log. It skips builds whose log already
finished, so an interrupted sweep resumes where it stopped. `report` reads every log under OUT.

Score = waves cleared (a victory counts all waves). Read results as relative: they measure the
Autoplay bot (SteeringBot + ShopBot), not a human player.

Item ranking needs --shop=random runs (smart shopping picks items by its own weights). To limit
survivorship bias (long runs visit more shops, so they buy more of everything), it only counts items
bought in the first --item-window shops, among runs that got that far.
"""
import argparse
import itertools
import os
import re
import shlex
import statistics
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MAC_GODOT = "/Applications/Godot_mono.app/Contents/MacOS/Godot"
FRAMES_PER_RUN = 90000  # 20 waves of up to 60s at 60 fps, plus shops; a safety net only


def godot():
    if os.environ.get("GODOT"):
        return os.environ["GODOT"]
    return MAC_GODOT if Path(MAC_GODOT).exists() else "godot"


def class_spells():
    """{class id: [spell file names in list order]} read from data/classes/*.tres."""
    result = {}
    for path in sorted((ROOT / "data/classes").glob("*.tres")):
        text = path.read_text()
        ids = dict(re.findall(r'path="res://data/spells/([^"]+)\.tres" id="([^"]+)"', text))
        by_id = {v: k for k, v in ids.items()}
        spells_line = re.search(r"^Spells = .*$", text, re.M)
        class_id = re.search(r'^Id = "([^"]+)"', text, re.M)
        if not spells_line or not class_id:
            continue
        order = re.findall(r'ExtResource\("([^"]+)"\)', spells_line.group(0))
        result[class_id.group(1)] = [by_id[i] for i in order if i in by_id]
    return result


def finished(log):
    return log.exists() and "run_end" in log.read_text(errors="replace").rsplit("run_start", 1)[-1]


def run(args, extra):
    out = Path(args.out)
    spells = class_spells()
    classes = args.classes.split(",") if args.classes else list(spells)
    jobs = []
    for cls in classes:
        if cls not in spells:
            sys.exit(f"unknown class {cls} (have: {', '.join(spells)})")
        for combo in itertools.combinations(spells[cls], args.picks):
            log = out / cls / ("+".join(combo) + ".log")
            if not finished(log):
                jobs.append((cls, combo, log))

    total = sum(len(list(itertools.combinations(spells[c], args.picks))) for c in classes)
    print(f"{len(jobs)} of {total} builds to play ({args.runs} runs each, {args.jobs} at a time)", flush=True)

    def play(job):
        cls, combo, log = job
        log.parent.mkdir(parents=True, exist_ok=True)
        cmd = [godot(), "--headless", "--path", str(ROOT), "--fixed-fps", "60", "--disable-vsync",
               "--quit-after", str(args.runs * FRAMES_PER_RUN), "--",
               "--autoplay", f"--class={cls}", f"--spells={','.join(combo)}", f"--runs={args.runs}",
               f"--seed={args.seed}", "--move=smart", "--autocast", f"--shop={args.shop}", *extra]
        with open(log, "w") as f:
            f.write("# " + shlex.join(cmd) + "\n")
            f.flush()
            subprocess.run(cmd, stdout=f, stderr=subprocess.STDOUT)
        return job

    done = 0
    with ThreadPoolExecutor(args.jobs) as pool:
        for cls, combo, _ in pool.map(play, jobs):
            done += 1
            print(f"[{done}/{len(jobs)}] {cls} {'+'.join(combo)}", flush=True)


FIELD = re.compile(r'(\w+)=("([^"]*)"|\S+)')


def fields(line):
    return {k: (q if q is not None and v.startswith('"') else v) for k, v, q in FIELD.findall(line)}


def parse_runs(out):
    """Every finished run under OUT: class, spells, score, buys, damage by source."""
    runs = []
    for log in sorted(Path(out).rglob("*.log")):
        cur = None
        for line in log.read_text(errors="replace").splitlines():
            if "[autoplay]" not in line:
                continue
            event = line.split("[autoplay] ", 1)[1].split(" ", 1)[0]
            f = fields(line)
            if event == "run_start":
                cur = {"class": f["class"], "spells": tuple(sorted(f["spells"].split(","))), "seed": f.get("seed"),
                       "waves": int(f.get("waves", 5)), "buys": [], "damage": {}, "shop": 0}
            elif cur is None:
                continue
            elif event == "wave_end":
                cur["shop"] = int(f["wave"])
            elif event == "buy" and "rarity" in f:
                cur["buys"].append((f["item"], cur["shop"]))
            elif event == "run_end":
                cur["victory"] = f["result"] == "victory"
                cur["cleared"] = int(f["wave"]) if cur["victory"] else int(f["wave"]) - 1
                cur["kills"] = int(f["kills"])
            elif event == "damage" and "cleared" in cur:
                for part in filter(None, f.get("by", "").split(",")):
                    name, _, amount = part.rpartition(":")
                    cur["damage"][name] = float(amount)
                runs.append(cur)
                cur = None
    return runs


def mean(xs):
    return statistics.fmean(xs) if xs else float("nan")


def report(args):
    runs = parse_runs(args.out)
    if not runs:
        sys.exit("no finished runs found")
    by_class = {}
    for r in runs:
        by_class.setdefault(r["class"], []).append(r)

    results = {"classes": {}}
    for cls, rs in sorted(by_class.items()):
        waves = rs[0]["waves"]
        builds = {}
        for r in rs:
            builds.setdefault(r["spells"], []).append(r)
        build_rows = sorted(({"spells": list(k), "runs": len(v), "cleared": mean([r["cleared"] for r in v]),
                              "win": mean([r["victory"] for r in v]), "kills": mean([r["kills"] for r in v])}
                             for k, v in builds.items()), key=lambda b: -b["cleared"])

        # Spell lift: average score of builds with the spell minus builds without it.
        all_spells = sorted({s for k in builds for s in k})
        spell_rows = []
        for s in all_spells:
            with_s = [r["cleared"] for r in rs if s in r["spells"]]
            without = [r["cleared"] for r in rs if s not in r["spells"]]
            share = mean([r["damage"].get(s, 0) / max(sum(r["damage"].values()), 1) for r in rs if s in r["spells"]])
            spell_rows.append({"spell": s, "with": mean(with_s), "without": mean(without),
                               "lift": mean(with_s) - mean(without), "damage_share": share, "runs": len(with_s)})
        spell_rows.sort(key=lambda x: -x["lift"])

        results["classes"][cls] = {"waves": waves, "runs": len(rs), "cleared": mean([r["cleared"] for r in rs]),
                                   "win": mean([r["victory"] for r in rs]), "builds": build_rows, "spells": spell_rows}

        print(f"\n== {cls}: {len(rs)} runs, {len(builds)} builds, avg cleared {mean([r['cleared'] for r in rs]):.2f}/{waves}, "
              f"win {100 * mean([r['victory'] for r in rs]):.0f}%")
        print(f"  {'spell':<22}{'with':>7}{'without':>9}{'lift':>7}{'dmg%':>7}")
        for x in spell_rows:
            print(f"  {x['spell']:<22}{x['with']:>7.2f}{x['without']:>9.2f}{x['lift']:>+7.2f}{100 * x['damage_share']:>6.0f}%")
        print("  best builds:")
        for b in build_rows[:5]:
            print(f"    {b['cleared']:5.2f}  win {100 * b['win']:3.0f}%  {', '.join(b['spells'])}")
        print("  worst builds:")
        for b in build_rows[-3:]:
            print(f"    {b['cleared']:5.2f}  win {100 * b['win']:3.0f}%  {', '.join(b['spells'])}")

    # Items: bought in the first W shops, among runs that cleared at least W waves.
    w = args.item_window
    eligible = [r for r in runs if r["cleared"] >= w]
    # Lift is measured within each class, then averaged weighted by buyers, so a class-only item
    # (a Necromancer weapon) isn't blamed for its class being weak.
    items = sorted({name for r in eligible for name, shop in r["buys"] if shop <= w})
    item_rows = []
    for name in items:
        bought = lambda r: any(n == name and s <= w for n, s in r["buys"])
        lifts, buyers, classes = [], 0, []
        for cls in sorted({r["class"] for r in eligible}):
            has = [r["cleared"] for r in eligible if r["class"] == cls and bought(r)]
            lacks = [r["cleared"] for r in eligible if r["class"] == cls and not bought(r)]
            if has and lacks:
                lifts.append((mean(has) - mean(lacks), len(has)))
                buyers += len(has)
                classes.append(cls)
        if buyers >= 5:
            item_rows.append({"item": name, "runs": buyers, "classes": classes,
                              "lift": sum(l * n for l, n in lifts) / buyers})
    item_rows.sort(key=lambda x: -x["lift"])
    results["items"] = {"window": w, "eligible_runs": len(eligible), "rows": item_rows}
    if item_rows:
        print(f"\n== items bought in shops 1-{w} ({len(eligible)} runs that cleared {w}+ waves; 5+ buyers; lift within class)")
        for x in item_rows:
            cls = ",".join(x["classes"]) if len(x["classes"]) < 4 else "all"
            print(f"  {x['item']:<24}{x['runs']:>5} runs  {x['lift']:+6.2f}  {cls}")

    if args.json:
        import json
        Path(args.json).write_text(json.dumps(results, indent=1))
        print(f"\nwrote {args.json}")


def main():
    argv = sys.argv[1:]
    extra = []
    if "--" in argv:
        i = argv.index("--")
        argv, extra = argv[:i], argv[i + 1:]
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("run")
    r.add_argument("out")
    r.add_argument("--classes", default="")
    r.add_argument("--runs", type=int, default=5)
    r.add_argument("--seed", type=int, default=1)
    r.add_argument("--shop", default="smart")
    r.add_argument("--picks", type=int, default=3)
    r.add_argument("-j", "--jobs", type=int, default=8)
    s = sub.add_parser("report")
    s.add_argument("out")
    s.add_argument("--json", default="")
    s.add_argument("--item-window", type=int, default=4)
    args = p.parse_args(argv)
    run(args, extra) if args.cmd == "run" else report(args)


if __name__ == "__main__":
    main()
