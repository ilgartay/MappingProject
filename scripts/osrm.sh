#!/usr/bin/env bash
#
# OSRM konteynerini baslatir / durdurur / durumunu gosterir.
#
#   ./scripts/osrm.sh start|stop|status
#
# Ilk kurulum icin osrm-setup.sh; bu betik yalnizca hazir konteyneri
# yonetiyor. Konteyner "--restart unless-stopped" ile calisiyor, yani
# Docker acildiginda kendiliginden geri geliyor.
set -euo pipefail

CONTAINER="mapproject-osrm"
PORT="${OSRM_PORT:-5001}"

case "${1:-status}" in
  start)
    docker start "$CONTAINER" >/dev/null
    echo "OSRM calisiyor: http://localhost:$PORT"
    ;;
  stop)
    docker stop "$CONTAINER" >/dev/null
    echo "OSRM durduruldu."
    ;;
  status)
    if docker ps --filter "name=$CONTAINER" --format '{{.Names}}' | grep -q "$CONTAINER"; then
      # Ankara icinde kisa bir rota: sunucu gercekten cevap veriyor mu?
      code=$(curl -s -o /dev/null -w '%{http_code}' \
        "http://localhost:$PORT/route/v1/driving/32.8541,39.9208;32.8597,40.0158")
      echo "OSRM ayakta (HTTP $code) - http://localhost:$PORT"
    else
      echo "OSRM kapali. Baslatmak icin: ./scripts/osrm.sh start"
    fi
    ;;
  *)
    echo "kullanim: $0 start|stop|status" >&2
    exit 1
    ;;
esac
