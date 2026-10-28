#!/usr/bin/env bash
# Batch balance runs: plays N seeded Autoplay runs per class and movement mode, then prints a
# table of win rate, average wave reached, kills, hits taken and gold. One Godot launch per
# class × mode (using --runs=N, so run k uses seed+k); launches run in parallel.
#
#   tools/simulate.sh                                   # all classes, smart movement, 20 runs each
#   tools/simulate.sh -n 50 -m circle,smart             # compare movement modes
#   tools/simulate.sh -c fighter,mage -- --shop=skip    # anything after -- goes to every run
#
# Options: -n RUNS (default 20)  -s SEED (default 1)  -c CLASSES (default fighter,mage,warlock,necromancer)
#          -m MODES (default smart)  -j JOBS (parallel launches, default 8)  -o DIR (keep raw logs there)
# Works with macOS bash 3.2. Needs $GODOT (the Godot .NET binary) and a built C# assembly (dotnet build).
set -euo pipefail

runs=20 seed=1 classes=fighter,mage,warlock,necromancer modes=smart jobs=8 out=""
while getopts "n:s:c:m:j:o:" opt; do
  case $opt in
    n) runs=$OPTARG ;; s) seed=$OPTARG ;; c) classes=$OPTARG ;; m) modes=$OPTARG ;;
    j) jobs=$OPTARG ;; o) out=$OPTARG ;; *) sed -n 2,12p "$0"; exit 1 ;;
  esac
done
shift $((OPTIND - 1))
[[ ${1:-} == "--" ]] && shift
extra=("$@")

godot=${GODOT:-godot}
cd "$(dirname "$0")/.."
logs=${out:-$(mktemp -d)}
mkdir -p "$logs"

run_one() {
  local class=$1 mode=$2
  # A full 20-wave run is ~70k frames (~35s at about 30x real time); this is only a safety quit.
  "$godot" --headless --path . --fixed-fps 60 --disable-vsync --quit-after $((runs * 90000)) -- \
    --autoplay --class="$class" --move="$mode" --seed="$seed" --runs="$runs" ${extra[@]+"${extra[@]}"} \
    > "$logs/$class.$mode.log" 2>&1 || echo "warning: $class/$mode exited with $?" >&2
}

pending=0
for class in ${classes//,/ }; do
  for mode in ${modes//,/ }; do
    run_one "$class" "$mode" &
    # Batches of $jobs (macOS ships bash 3.2, which has no `wait -n`).
    if (( ++pending >= jobs )); then wait; pending=0; fi
  done
done
wait

printf "%-12s %-7s %5s %6s %7s %6s %6s %6s %s\n" class move runs win% wave kills hits gold errors
for class in ${classes//,/ }; do
  for mode in ${modes//,/ }; do
    log="$logs/$class.$mode.log"
    errors=$(grep -c "\[autoplay\] error" "$log" || true)
    grep "\[autoplay\] run_end" "$log" | awk -v class="$class" -v mode="$mode" -v errors="$errors" '
      {
        for (i = 1; i <= NF; i++) { split($i, kv, "="); v[kv[1]] = kv[2] }
        n++; wins += (v["result"] == "victory"); wave += v["wave"]; kills += v["kills"]; hits += v["hits"]; gold += v["gold"]
      }
      END {
        if (n == 0) { printf "%-12s %-7s %5d %6s %7s %6s %6s %6s %s\n", class, mode, 0, "-", "-", "-", "-", "-", errors; exit }
        printf "%-12s %-7s %5d %5.0f%% %7.2f %6.0f %6.1f %6.0f %s\n", class, mode, n, 100 * wins / n, wave / n, kills / n, hits / n, gold / n, errors
      }'
  done
done
if [[ -n $out ]]; then echo "raw logs: $logs"; fi
