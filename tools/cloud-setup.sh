#!/usr/bin/env bash
# Cloud / headless Linux setup for ArenaShooter: installs the .NET SDK and the Godot .NET
# editor binary, enough to build C#, run the smoke test and run Autoplay. No GPU, display
# or Movie Maker support (see CLAUDE.md, "Testing in the cloud / headless").
#
# Self-contained: paste it into a cloud environment's setup script, or run it from the repo
# root. Run from the repo root, it reads both versions from ArenaShooter.csproj
# (Sdk="Godot.NET.Sdk/<version>", <TargetFramework>net<channel></TargetFramework>), so
# upgrading Godot or .NET is a .csproj change. Elsewhere it falls back to the defaults below,
# which should match the .csproj.
set -euo pipefail

GODOT_VERSION_DEFAULT="4.7.2"
DOTNET_CHANNEL_DEFAULT="10.0"
if [ -f ArenaShooter.csproj ]; then
  GODOT_VERSION_DEFAULT="$(sed -n 's#.*Godot.NET.Sdk/\([0-9.]*\).*#\1#p' ArenaShooter.csproj | head -1)"
  DOTNET_CHANNEL_DEFAULT="$(sed -n 's#.*<TargetFramework>net\([0-9.]*\)</TargetFramework>.*#\1#p' ArenaShooter.csproj | head -1)"
fi
GODOT_VERSION="${GODOT_VERSION:-$GODOT_VERSION_DEFAULT}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-$DOTNET_CHANNEL_DEFAULT}"
INSTALL_DIR="${INSTALL_DIR:-/opt/godot}"

START_TIME=$SECONDS
log() { echo "[cloud-setup] [$((SECONDS - START_TIME))s] $*"; }
trap 'log "FAILED (exit $?) at line $LINENO: $BASH_COMMAND"' ERR

SUDO=""
if [ "$(id -u)" -ne 0 ]; then
  if command -v sudo >/dev/null 2>&1; then SUDO="sudo"; else
    log "not root and no sudo; installing under \$HOME instead"
    INSTALL_DIR="$HOME/.local/godot"
  fi
fi
can_write_system() { [ "$(id -u)" -eq 0 ] || [ -n "$SUDO" ]; }

log "Godot ${GODOT_VERSION}, .NET ${DOTNET_CHANNEL}"

# --- System packages: curl/unzip for the downloads, ICU for .NET ------------------------
# Only installs what's missing, and an apt failure isn't fatal: there are fallbacks below.
# (apt's "debconf: delaying package configuration" line is harmless.)
has_icu() { ldconfig -p 2>/dev/null | grep -q libicuuc; }
MISSING=()
for tool in curl unzip; do command -v "$tool" >/dev/null 2>&1 || MISSING+=("$tool"); done
[ -e /etc/ssl/certs/ca-certificates.crt ] || MISSING+=(ca-certificates)
has_icu || MISSING+=(libicu-dev)
if [ ${#MISSING[@]} -gt 0 ] && command -v apt-get >/dev/null 2>&1 && can_write_system; then
  log "installing system packages: ${MISSING[*]}"
  export DEBIAN_FRONTEND=noninteractive
  if ! { $SUDO apt-get update -qq && $SUDO apt-get install -y -qq --no-install-recommends "${MISSING[@]}"; } \
      >/tmp/cloud-setup-apt.log 2>&1; then
    log "apt-get failed (continuing with fallbacks); last lines of /tmp/cloud-setup-apt.log:"
    tail -n 15 /tmp/cloud-setup-apt.log | sed 's/^/    /'
  fi
fi
command -v curl >/dev/null 2>&1 || { log "curl is required and couldn't be installed"; exit 1; }
if ! command -v unzip >/dev/null 2>&1 && ! command -v python3 >/dev/null 2>&1; then
  log "need unzip or python3 to unpack Godot"; exit 1
fi
if ! has_icu; then
  log "no ICU library; running .NET in invariant globalization mode"
  export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
fi

# --- .NET SDK ----------------------------------------------------------------------------
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
if ! "$DOTNET_ROOT/dotnet" --list-sdks 2>/dev/null | grep -q "^${DOTNET_CHANNEL}\."; then
  log "installing .NET SDK ${DOTNET_CHANNEL} into $DOTNET_ROOT"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_ROOT" >/dev/null
fi
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# --- Godot .NET editor binary ------------------------------------------------------------
case "$(uname -m)" in
  x86_64|amd64) GODOT_ARCH="x86_64"; GODOT_BIN_SUFFIX="x86_64" ;;
  aarch64|arm64) GODOT_ARCH="arm64"; GODOT_BIN_SUFFIX="arm64" ;;
  *) log "unsupported architecture $(uname -m)"; exit 1 ;;
esac
GODOT_NAME="Godot_v${GODOT_VERSION}-stable_mono_linux_${GODOT_ARCH}"
GODOT_BIN="$INSTALL_DIR/$GODOT_NAME/Godot_v${GODOT_VERSION}-stable_mono_linux.${GODOT_BIN_SUFFIX}"

if [ ! -x "$GODOT_BIN" ]; then
  log "installing Godot ${GODOT_VERSION} .NET (${GODOT_ARCH})"
  curl -fsSL "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/${GODOT_NAME}.zip" \
    -o /tmp/godot.zip
  ROOT_CMD=""; [ "$INSTALL_DIR" = "/opt/godot" ] && ROOT_CMD="$SUDO"
  $ROOT_CMD mkdir -p "$INSTALL_DIR"
  # Keep GodotSharp/ next to the binary.
  if command -v unzip >/dev/null 2>&1; then
    $ROOT_CMD unzip -q -o /tmp/godot.zip -d "$INSTALL_DIR"
  else
    $ROOT_CMD python3 -m zipfile -e /tmp/godot.zip "$INSTALL_DIR"
  fi
  $ROOT_CMD chmod +x "$GODOT_BIN"
  rm -f /tmp/godot.zip
fi

# --- Put `godot` and `dotnet` on PATH for later shells -----------------------------------
if can_write_system; then
  $SUDO ln -sf "$GODOT_BIN" /usr/local/bin/godot
  $SUDO ln -sf "$DOTNET_ROOT/dotnet" /usr/local/bin/dotnet
else
  mkdir -p "$HOME/.local/bin"
  ln -sf "$GODOT_BIN" "$HOME/.local/bin/godot"
  ln -sf "$DOTNET_ROOT/dotnet" "$HOME/.local/bin/dotnet"
fi
PROFILE_LINES="export DOTNET_ROOT=\"$DOTNET_ROOT\"
export PATH=\"\$HOME/.local/bin:\$DOTNET_ROOT:\$PATH\"
export GODOT=godot
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1"
[ -n "${DOTNET_SYSTEM_GLOBALIZATION_INVARIANT:-}" ] && PROFILE_LINES+=$'\nexport DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1'
for rc in "$HOME/.bashrc" "$HOME/.profile"; do
  grep -q "ArenaShooter cloud setup" "$rc" 2>/dev/null || printf '\n# ArenaShooter cloud setup\n%s\n' "$PROFILE_LINES" >> "$rc"
done
export PATH="$HOME/.local/bin:$PATH" GODOT=godot

# --- Warm caches when run from the repo root (NuGet packages, Godot import cache) ----------
if [ -f ArenaShooter.csproj ]; then
  log "restoring and building the project"
  dotnet build -v quiet -nologo
  log "importing assets (generates .godot/)"
  godot --headless --path . --import >/dev/null 2>&1 || true
fi

log "done: $(dotnet --version) / $(godot --version 2>/dev/null | tail -1)"
