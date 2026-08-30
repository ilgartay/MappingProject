# OSRM - Otomatik Rota Üretimi

Duraklardan geçen sürüş rotasını hesaplayan açık kaynak yönlendirme
sunucusu. Docker'da, tamamen yerelde çalışıyor.

## Neden ayrı bir sunucu

Duraklar arasına düz çizgi çekmek kolay ama yol değil. "100 numaralı hat
14 dakika sürüyor" diyebilmek için asfaltı, tek yönleri ve dönüş
kısıtlarını bilen bir motora ihtiyaç var. OSRM OpenStreetMap verisini
önceden işleyip bu soruyu milisaniyelerde cevaplıyor.

## Kurulum

```bash
./scripts/osrm-setup.sh      # ilk kurulum: indirme + ön işleme + başlatma
./scripts/osrm.sh status     # ayakta mı
./scripts/osrm.sh start|stop
```

Betik her adımı atlanabilir yapıyor: çıktı dosyası varsa o adım
çalışmıyor, yani tekrar çalıştırmak 40 dakikayı baştan almıyor.

Adımlar:

1. **İndirme** - Geofabrik'ten `turkey-latest.osm.pbf` (~614 MB).
2. **osrm-extract** - yol ağını çıkarır (`car.lua` profili). En uzun ve
   en aç gözlü adım.
3. **osrm-partition** + **osrm-customize** - MLD (multi-level Dijkstra)
   için hiyerarşi kurar.
4. **osrm-routed** - HTTP sunucusu, `--algorithm mld`.

### Tuzaklar

- **Bellek.** İlk denemede `osrm-extract` 137 (SIGKILL) ile öldü: Docker
  Desktop'ın VM'i 8 GB'tı ve Türkiye çıkarımı sığmadı. 16 GB'a
  çıkarılınca geçti. Docker Desktop ayarı `MemoryMiB`; **Docker kapalıyken**
  yazılmalı, çünkü uygulama kapanırken kendi ayarlarını dosyaya geri
  yazıp değişikliği siliyor.
- **Port 5001.** Konteynerin içi 5000 ama macOS'ta 5000'i AirPlay
  Receiver tutuyor, dışarıya 5001'den açıyoruz.
- **Koordinat sırası boylam,enlem.** WFS'teki eksen sırası tuzağının ters
  yönü: orada EPSG:4326 enlem-önceydi, burada sunucu açıkça boylam-önce
  istiyor. Ters gönderilirse hata dönmüyor - `NoRoute` dönüyor ya da
  denizin ortasından bir rota çiziyor.
- **`overview=full`.** Varsayılan `simplified` uzun hatlarda çizgiyi
  kırpıyor ve yol asfalttan sapmış görünüyor.

## Uygulamadaki yeri

```
React  ->  API (/api/Transport/routes/{id}/route)  ->  OSRM  ->  PostGIS
```

Tarayıcı OSRM'e doğrudan gitmiyor. Sebebi GeoServer'daki ile aynı değil -
OSRM'de yetki filtresi yok - ama sonuç veritabanına yazılacağı için
isteği zaten sunucu atmalı.

- `OsrmClient` HTTP isteğini atıp GeoJSON çizgiyi `LineString`'e çeviriyor.
- `TransportService.BuildRouteAsync` sonucu `guzergah` tablosuna yazıyor:
  `rota_geom`, `rota_mesafe`, `rota_sure`, `rota_tarih`.
- Rota saklanıyor, her açılışta yeniden hesaplanmıyor: OSRM kapalıyken de
  hat haritada görünsün.

### Otomatik güncelleme

Durak eklenince, silinince, taşınınca ya da **sırası değişince** rota
yeniden hesaplanıyor. Yalnızca daha önce rotası üretilmiş güzergahlar
için: hiç "Rota Oluştur" denmemiş bir hatta durak eklemek OSRM'e istek
atmıyor.

Hesaplama başarısızsa eski çizgi **siliniyor**. Bırakmak daha zararsız
görünüyor ama çizgi artık durak sırasına uymuyor; boş harita "rota yok"
der, eski rota ise yanlış bir yolu doğruymuş gibi gösterir. Sıralama yine
de kaydediliyor, sebep `routeWarning` ile panele düşüyor.

## Haritada gösterim

- Rota, güzergahın renginde kalın çizgi. Üzerinde yön okları
  (`osrmRouteStyle`), ekranda ~90 pikselde bir, en fazla 60 tane.
- Ok açısı `Math.atan2(dx, dy)` - alışılmış `atan2(dy, dx)` değil:
  OpenLayers'ta döndürme yukarıdan başlayıp saat yönünde ölçülüyor.
- Rotası olmayan hatlarda durakları birleştiren **kesikli** yardımcı
  çizgi çiziliyor; gerçek yol olmadığı belli olsun diye.
- Panelde her güzergahın yanında bir onay kutusu var: katman kontrolü
  gibi hattı ve duraklarını haritadan kaldırıyor.
