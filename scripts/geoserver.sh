#!/bin/sh
# GeoServer'ı projenin veri dizini ile başlatır.
#
# Neden ayrı betik: Homebrew'un openjdk@21 paketi "keg-only", yani `java`
# komutu PATH'e eklenmiyor. Homebrew'un kendi `geoserver` betiği ise düz
# `java` çağırdığı için onu doğrudan çalıştırınca "Unable to locate a Java
# Runtime" hatası alınıyor. Burada JAVA_HOME'u kendimiz veriyoruz.
#
# Kullanım:  ./scripts/geoserver.sh
# Adres:     http://localhost:8080/geoserver   (admin / geoserver)

set -e

JAVA_HOME="${JAVA_HOME:-/opt/homebrew/opt/openjdk@21}"
GEOSERVER_HOME="${GEOSERVER_HOME:-/opt/homebrew/opt/geoserver/libexec}"
# Katmanlar, workspace ve store tanımları bu dizinde saklanıyor; sürüm
# yükseltmelerinde silinmesin diye repo ve Cellar dışında duruyor.
GEOSERVER_DATA_DIR="${GEOSERVER_DATA_DIR:-$HOME/geoserver_data}"

export PATH="$JAVA_HOME/bin:$PATH"

cd "$GEOSERVER_HOME"

# -Djava.awt.headless=true: GeoServer'in arayuzu yok, tarayicidan
# kullaniliyor. Bu bayrak olmadan macOS surecin Dock'ta bir Java simgesi
# gostermesine yol aciyor; simgeye tiklaninca acilacak bir pencere
# olmadigi icin kullanici "acilmiyor" saniyor.
exec java -Djava.awt.headless=true \
  -DGEOSERVER_DATA_DIR="$GEOSERVER_DATA_DIR" \
  -jar start.jar
