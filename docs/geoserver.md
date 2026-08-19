# GeoServer Entegrasyonu

## Kavramlar

GeoServer'ın yapısı iç içe dört kutu gibi düşünülebilir:

| Kavram | Ne demek | Bizdeki karşılığı |
|---|---|---|
| **Workspace** | Katmanları gruplayan isim alanı. Aynı addaki iki tabloyu ayrı projelerde tutabilmeyi sağlar. | `mapproject` |
| **Store** | Verinin fiziksel kaynağı: bir veritabanı bağlantısı ya da dosya. | `mapdb` (PostGIS bağlantısı) |
| **Layer** | Store içindeki tek bir tablonun yayınlanmış hali. | `tbl_point`, `tbl_line`, `tbl_polygon` |
| **Layer Group** | Birden çok katmanı tek isimle sunma. | Kullanmıyoruz |

Servisler ise aynı katmanı iki farklı biçimde dışarı verir:

- **WMS** (Web Map Service) — Katmanı **sunucuda çizip resim** olarak döner.
  İstemci PNG alır, veriyi görmez. Milyonlarca kayıtta hızlıdır, ama gelen
  şey resim olduğu için tıklanabilir/düzenlenebilir değildir.
- **WFS** (Web Feature Service) — Katmanı **veri** olarak döner (GeoJSON, GML).
  Her kaydın geometrisi ve öznitelikleri gelir; istemci kendi stilini uygular,
  üstüne tıklayabilir, düzenleyebilir.

Bu projede **WFS** kullanıyoruz: çizimlerin rengi kullanıcıya ait bir öznitelik
ve kullanıcı çizime tıklayıp düzenleyebiliyor. WMS resim döndüğü için ikisi de
mümkün olmazdı.

## Mimari

Ödevin istediği akış:

```
React  ──►  MapProject.API  ──►  GeoServer (WFS)  ──►  PostGIS
```

API artık okuma için veritabanına doğrudan SELECT atmıyor. Yazma işlemleri
(ekle/güncelle/sil) EF Core üzerinden devam ediyor; çünkü kaydetmeden önce
coğrafi yetki kontrolü, izleme kolonlarının damgalanması ve soft delete gibi
iş kuralları çalışıyor.

İlgili dosyalar:

- `MapProject.Business/GeoServer/GeoServerFeatureReader.cs` — WFS isteği ve GeoJSON→DTO çevirisi
- `MapProject.Business/Settings/GeoServerSettings.cs` — adres, workspace, katman adları
- `MapProject.Business/Services/FeatureService.cs` — `GetAllAsync` artık okuyucuyu kullanıyor

## Dikkat edilecek üç nokta

Üçü de **hata vermeden yanlış sonuç** ürettiği için ayrıca yazıldı.

**1. Silinmiş kayıtları biz elemek zorundayız.** EF'in global sorgu filtresi
(`is_deleted = false`) GeoServer'da yok; GeoServer tabloyu ham haliyle okuyor.
Filtreyi CQL olarak biz gönderiyoruz:

```
cql_filter=is_deleted = false AND inserted_user_id = 2
```

**2. Filtre parametresi küçük harfle yazılmalı.** Bu kurulumda (GeoServer 3.0.1)
tamamı büyük `CQL_FILTER` **sessizce yok sayılıyor** — hata vermiyor, filtresiz
sonuç dönüyor. Yani yanlış yazım "veri gelmedi" değil, "herkesin verisi geldi"
sonucunu doğuruyor. `cql_filter` çalışıyor.

**3. WFS sürümü 1.0.0 kullanıyoruz — koordinat ekseni sırası yüzünden.**

WFS 1.0.0'da `EPSG:4326` **boylam/enlem** demek. OGC, 1.1.0 ile birlikte bunu
otoritenin tanımına yani **enlem/boylam**'a çevirdi. Verimiz ve GeoJSON çıktısı
boylam/enlem olduğu için 2.0.0'da uzamsal filtreler hiçbir kayıtla eşleşmiyor:

```
# ayni sorgu, sadece surum farkli — nokta 10 = POINT(32.86 39.93)
version=1.0.0 + INTERSECTS(geom, POINT(32.86 39.93))  ->  1 kayit  ✓
version=2.0.0 + INTERSECTS(geom, POINT(32.86 39.93))  ->  0 kayit  ✗
version=2.0.0 + INTERSECTS(geom, POINT(39.93 32.86))  ->  1 kayit  (ters yazim)
```

`srsName` göndermek bunu düzeltmiyor; filtrenin nasıl yorumlandığı sürüme bağlı.
Öznitelik filtreleri (`id = 10`) eksenden etkilenmediği için sorun yalnızca
uzamsal filtrelerde ortaya çıkıyordu — analiz aracı sessizce hep 0 döndürüyordu.

Sürüm 1.0.0'da parametrenin adı da `typeName` (tekil); 2.0.0'da `typeNames`.

## Kurulum

```bash
brew install openjdk@21 geoserver
cp -R /opt/homebrew/opt/geoserver/libexec/data_dir ~/geoserver_data
./scripts/geoserver.sh                 # http://localhost:8080/geoserver
./scripts/geoserver-setup.sh           # workspace + store + 3 katman
```

Ardından API'nin GeoServer şifresini bilmesi gerekiyor:

```bash
dotnet user-secrets set "GeoServer:Password" "geoserver" --project MapProject.API
```

Yönetim arayüzü: <http://localhost:8080/geoserver> (`admin` / `geoserver`)

## Elle doğrulama

```bash
# WFS: veri olarak (uygulamanin kullandigi bicim)
curl -s -G "http://localhost:8080/geoserver/mapproject/ows" \
  --data-urlencode "service=WFS" --data-urlencode "version=1.0.0" \
  --data-urlencode "request=GetFeature" \
  --data-urlencode "typeName=mapproject:tbl_point" \
  --data-urlencode "outputFormat=application/json" \
  --data-urlencode "sortBy=id" \
  --data-urlencode "cql_filter=is_deleted = false AND INTERSECTS(geom, POINT(32.86 39.93))"

# WMS: resim olarak
curl -s -G "http://localhost:8080/geoserver/mapproject/wms" \
  --data-urlencode "service=WMS" --data-urlencode "version=1.3.0" \
  --data-urlencode "request=GetMap" \
  --data-urlencode "layers=mapproject:tbl_point" \
  --data-urlencode "bbox=35.0,25.0,43.0,45.0" \
  --data-urlencode "crs=EPSG:4326" \
  --data-urlencode "width=400" --data-urlencode "height=300" \
  --data-urlencode "format=image/png" -o nokta.png
```
