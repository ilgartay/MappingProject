#!/usr/bin/env bash
#
# OSRM'i Docker uzerinde ayaga kaldirir: OSM cikarimini indirir,
# on isler ve yonlendirme sunucusunu baslatir.
#
#   ./scripts/osrm-setup.sh
#
# Her adim idempotent: cikti dosyasi varsa o adim atlaniyor, betigi
# tekrar calistirmak 40 dakikalik on islemeyi bastan yapmiyor.
#
# Neden MLD (multi-level Dijkstra): on isleme CH'ye gore hizli ve
# osrm-customize tek basina saniyeler suruyor. Bu projede yol agi
# degismiyor, ama bir kez kurup unutmak istiyoruz.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DATA="$ROOT/osrm"
PBF_URL="https://download.geofabrik.de/europe/turkey-latest.osm.pbf"
PBF="turkey-latest.osm.pbf"
BASE="turkey-latest"
IMAGE="ghcr.io/project-osrm/osrm-backend:latest"
CONTAINER="mapproject-osrm"

# 5000 macOS'ta AirPlay Receiver'da; 5001'e aliyoruz.
PORT="${OSRM_PORT:-5001}"

mkdir -p "$DATA"

run_osrm() {
  docker run --rm -t -v "$DATA:/data" "$IMAGE" "$@"
}

echo "==> Docker imaji"
docker image inspect "$IMAGE" >/dev/null 2>&1 || docker pull "$IMAGE"

echo "==> OSM cikarimi"
if [ -f "$DATA/$PBF" ]; then
  echo "    zaten var: $(du -h "$DATA/$PBF" | cut -f1)"
else
  curl -L --fail --progress-bar -o "$DATA/$PBF.indiriliyor" "$PBF_URL"
  mv "$DATA/$PBF.indiriliyor" "$DATA/$PBF"
fi

echo "==> osrm-extract (yol agini cikariyor, en uzun adim)"
if [ -f "$DATA/$BASE.osrm.ebg" ]; then
  echo "    atlandi"
else
  run_osrm osrm-extract -p /opt/car.lua "/data/$PBF"
fi

echo "==> osrm-partition"
if [ -f "$DATA/$BASE.osrm.partition" ]; then
  echo "    atlandi"
else
  run_osrm osrm-partition "/data/$BASE.osrm"
fi

echo "==> osrm-customize"
if [ -f "$DATA/$BASE.osrm.cell_metrics" ]; then
  echo "    atlandi"
else
  run_osrm osrm-customize "/data/$BASE.osrm"
fi

echo "==> osrm-routed :$PORT"
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$CONTAINER" --restart unless-stopped \
  -p "$PORT:5000" -v "$DATA:/data" "$IMAGE" \
  osrm-routed --algorithm mld "/data/$BASE.osrm" >/dev/null

echo "==> hazir: http://localhost:$PORT"
