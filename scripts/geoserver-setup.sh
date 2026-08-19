#!/bin/sh
# GeoServer'da workspace, PostGIS store ve uc katmani olusturur.
#
# Neden betik: bu tanimlar repoda degil, GeoServer'in kendi veri dizininde
# (~/geoserver_data) saklaniyor. Projeyi baska bir makineye kuran kisi ayni
# adimlari arayuzden tek tek tiklamak zorunda kalmasin diye REST ile yaziyoruz.
#
# Calistirmadan once GeoServer ayakta olmali:  ./scripts/geoserver.sh
# Tekrar calistirilabilir: zaten varsa 401/500 yerine "zaten var" yazar.

set -e

GS_URL="${GS_URL:-http://localhost:8080/geoserver}"
GS_USER="${GS_USER:-admin}"
GS_PASS="${GS_PASS:-geoserver}"
WORKSPACE="${WORKSPACE:-mapproject}"
STORE="${STORE:-mapdb}"

PG_HOST="${PG_HOST:-localhost}"
PG_PORT="${PG_PORT:-5432}"
PG_DB="${PG_DB:-mapdb}"
PG_USER="${PG_USER:-$(whoami)}"

REST="$GS_URL/rest"
AUTH="-u $GS_USER:$GS_PASS"

post() {
  # $1 = yol, $2 = govde, $3 = insan okunur ad
  code=$(curl -s $AUTH -XPOST -H "Content-Type: application/json" \
    -d "$2" -o /dev/null -w "%{http_code}" "$REST/$1")

  case "$code" in
    201)     echo "  olusturuldu: $3" ;;
    401)     echo "  HATA: kimlik reddedildi (GS_USER/GS_PASS)"; exit 1 ;;
    # GeoServer var olan kaydi workspace icin 409, store/katman icin 500
    # ile reddediyor. Ikisi de bizim icin "zaten var" demek.
    409|500) echo "  zaten var: $3" ;;
    *)       echo "  beklenmeyen cevap $code: $3" ;;
  esac
}

echo "GeoServer: $GS_URL"

echo "workspace..."
post "workspaces" \
  "{\"workspace\":{\"name\":\"$WORKSPACE\"}}" \
  "$WORKSPACE"

echo "PostGIS store..."
# "Expose primary keys" onemli: bu olmadan tablonun id kolonu ozniteliklerde
# gelmiyor, sadece "tbl_point.3" bicimindeki feature id'sinde gizli kaliyor.
post "workspaces/$WORKSPACE/datastores" \
  "{\"dataStore\":{\"name\":\"$STORE\",\"connectionParameters\":{\"entry\":[
    {\"@key\":\"dbtype\",\"\$\":\"postgis\"},
    {\"@key\":\"host\",\"\$\":\"$PG_HOST\"},
    {\"@key\":\"port\",\"\$\":\"$PG_PORT\"},
    {\"@key\":\"database\",\"\$\":\"$PG_DB\"},
    {\"@key\":\"schema\",\"\$\":\"public\"},
    {\"@key\":\"user\",\"\$\":\"$PG_USER\"},
    {\"@key\":\"Expose primary keys\",\"\$\":\"true\"},
    {\"@key\":\"validate connections\",\"\$\":\"true\"}
  ]}}}" \
  "$STORE -> $PG_DB@$PG_HOST:$PG_PORT"

echo "katmanlar..."
for table in tbl_point tbl_line tbl_polygon; do
  post "workspaces/$WORKSPACE/datastores/$STORE/featuretypes" \
    "{\"featureType\":{\"name\":\"$table\",\"srs\":\"EPSG:4326\",\"enabled\":true}}" \
    "$table"
done

echo "bitti. Kontrol: $GS_URL/$WORKSPACE/wfs?service=WFS&version=2.0.0&request=GetCapabilities"
