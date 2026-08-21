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

# Katmanlari tablodan degil SQL View'dan uretiyoruz. Kazanci: silinmis
# kayit filtresi tek yerde, view'in icinde duruyor. WMS ve WFS ayni view'i
# okudugu icin ikisinde ayri ayri filtre yazmak gerekmiyor; GeoServer
# arayuzunden bakan biri de kuralin nerede oldugunu goruyor.
echo "katmanlar (SQL View)..."
make_view() {
  table=$1; view=$2; gtype=$3
  sql="SELECT id, name, geom, color, inserted_user_id, inserted_date, modified_date, is_active FROM $table WHERE is_deleted = false"

  post "workspaces/$WORKSPACE/datastores/$STORE/featuretypes" \
    "{\"featureType\":{
       \"name\":\"$view\",
       \"nativeName\":\"$view\",
       \"title\":\"$view (SQL View)\",
       \"srs\":\"EPSG:4326\",
       \"enabled\":true,
       \"metadata\":{\"entry\":[{
         \"@key\":\"JDBC_VIRTUAL_TABLE\",
         \"virtualTable\":{
           \"name\":\"$view\",
           \"sql\":\"$sql\",
           \"escapeSql\":false,
           \"keyColumn\":\"id\",
           \"geometry\":{\"name\":\"geom\",\"type\":\"$gtype\",\"srid\":4326}
         }
       }]}
     }}" \
    "$view -> $table"
}

make_view tbl_point   vw_point   Point
make_view tbl_line    vw_line    LineString
make_view tbl_polygon vw_polygon Polygon

# SLD stilleri: WMS gosteriminin rengi/etiketi ve isi haritasi burada.
# Repoda geoserver/styles altinda duruyorlar, yani surum kontrolunde.
echo "stiller..."
SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)

for file in "$SCRIPT_DIR/../geoserver/styles"/*.sld; do
  [ -e "$file" ] || continue
  style=$(basename "$file" .sld)

  code=$(curl -s $AUTH -XPOST -H "Content-Type: application/vnd.ogc.sld+xml" \
    --data-binary "@$file" -o /dev/null -w "%{http_code}" \
    "$REST/workspaces/$WORKSPACE/styles?name=$style")

  # Var olan bir stil adi icin GeoServer 403 donuyor (yetki hatasi degil;
  # hemen asagidaki PUT'lar ayni kimlikle 200 aliyor).
  case "$code" in
    201)         echo "  olusturuldu: $style" ;;
    403|409|500) echo "  zaten var: $style" ;;
    *)           echo "  beklenmeyen cevap $code: $style" ;;
  esac
done

# Her katman kendi stilini varsayilan olarak kullansin; boylece WMS
# istegine STYLES yazmadan da dogru gorunum geliyor.
echo "varsayilan stiller..."
set_style() {
  code=$(curl -s $AUTH -XPUT -H "Content-Type: application/json" -o /dev/null -w "%{http_code}" \
    -d "{\"layer\":{\"defaultStyle\":{\"name\":\"$2\",\"workspace\":\"$WORKSPACE\"}}}" \
    "$REST/layers/$WORKSPACE:$1")
  echo "  $1 -> $2 ($code)"
}

set_style vw_point   mapproject_point
set_style vw_line    mapproject_line
set_style vw_polygon mapproject_polygon

echo "bitti. Kontrol: $GS_URL/$WORKSPACE/wfs?service=WFS&version=1.0.0&request=GetCapabilities"
