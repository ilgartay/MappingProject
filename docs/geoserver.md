# GeoServer Entegrasyonu

## Kavramlar

GeoServer'ın yapısı iç içe dört kutu gibi düşünülebilir:

| Kavram | Ne demek | Bizdeki karşılığı |
|---|---|---|
| **Workspace** | Katmanları gruplayan isim alanı. Aynı addaki iki tabloyu ayrı projelerde tutabilmeyi sağlar. | `mapproject` |
| **Store** | Verinin fiziksel kaynağı: bir veritabanı bağlantısı ya da dosya. | `mapdb` (PostGIS bağlantısı) |
| **Layer** | Store içindeki bir tablonun ya da SQL View'ın yayınlanmış hali. | `vw_point`, `vw_line`, `vw_polygon`, `vw_poi` |
| **Layer Group** | Birden çok katmanı tek isimle sunma. | Kullanmıyoruz |

Servisler ise aynı katmanı iki farklı biçimde dışarı verir:

- **WMS** (Web Map Service) — Katmanı **sunucuda çizip resim** olarak döner.
  İstemci PNG alır, veriyi görmez. Milyonlarca kayıtta hızlıdır, ama gelen
  şey resim olduğu için tıklanabilir/düzenlenebilir değildir.
- **WFS** (Web Feature Service) — Katmanı **veri** olarak döner (GeoJSON, GML).
  Her kaydın geometrisi ve öznitelikleri gelir; istemci kendi stilini uygular,
  üstüne tıklayabilir, düzenleyebilir.

Bu projede **ikisini de** kullanıyoruz, ama farklı işler için:

- **Genel gösterim → WMS.** Haritada gördüğün noktalar/çizgiler/poligonlar
  GeoServer'ın çizdiği bir PNG. Renkleri ve etiketleri sunucudaki SLD
  belirliyor, tarayıcı sadece resmi basıyor.
- **Çizim ve etkileşim → WFS.** Bir çizime tıklayıp adını, rengini veya
  konumunu değiştirdiğinde kullanılan veri WFS'ten geliyor. Resmin üstündeki
  bir şekli sürükleyip düzenlemek mümkün olmazdı.

Uygulamada bu ikisi üst üste duruyor: WMS katmanı görünen kısım, vektör
katmanı ise **tamamen saydam** ama tıklanabilir halde onun üstünde. Kullanıcı
bir kayda tıkladığında o kayıt görünür hale gelip düzenlenebiliyor.

## SQL View

Katmanlar tabloların doğrudan kendisi değil; her biri bir **SQL View**:

```sql
SELECT id, name, geom, color, inserted_user_id, inserted_date, modified_date, is_active
FROM tbl_point
WHERE is_deleted = false
```

Kazancı: silinmiş kayıt filtresi tek bir yerde duruyor. WMS de WFS de aynı
view'ı okuduğu için ikisinde ayrı ayrı filtre yazmak gerekmiyor, biri
unutulduğunda silinen çizimlerin geri gelmesi diye bir ihtimal kalmıyor.
View `is_deleted` kolonunu dışarı hiç vermiyor - GeoServer arayüzünden
bakan biri de kuralın nerede olduğunu görüyor.

Kullanıcı bazlı filtre ise view'a gömülemez, çünkü isteğe göre değişiyor.
Onu her istekte CQL olarak gönderiyoruz: `cql_filter=inserted_user_id = 2`.

### vw_poi — join yapan view

POI view'i diğer üçünden daha ileri gidiyor: kategori ağacını özyinelemeli
bir CTE ile gezip `Yeme-İçme → Restoran` biçiminde tam yolu üretiyor, ekleyen
kullanıcının adını da join'liyor. Böylece POI listesi **tek WFS isteğiyle**
tam geliyor - API'nin ayrıca kategori ve kullanıcı sorgusu atması gerekmiyor.

SQL'i [geoserver/sql/vw_poi.sql](../geoserver/sql/vw_poi.sql) dosyasında;
kurulum betiği JSON gövdesini `python3` ile kuruyor. Sebebi: SQL içinde
`"Users"` gibi çift tırnaklı tanımlayıcılar var ve kabuk bunları JSON'a
gömerken bozuyordu - tırnak kaçışını elle yönetmek yerine JSON'u üreten
bir araca bırakmak daha güvenli.

POI'ler kullanıcıya özel değil: bir eczanenin konumu herkes için aynı bilgi.
Bu yüzden `vw_poi` okunurken sahiplik filtresi eklenmiyor; `user_id` yalnızca
"kim ekledi" bilgisi olarak taşınıyor ve admin panelinde gösteriliyor.

POI'ler haritada WMS ile değil **vektör** olarak çiziliyor. Gerekçesi bir
önceki bölümdeki ayrımın aynısı: her POI'ye tıklanıp bilgi paneli açılması
gerekiyor, resim üzerinde bu mümkün değil.

## Isı haritası

"Isı Haritası Analizi" düğmesi, nokta katmanını `mapproject_heatmap`
stiliyle isteyen bir WMS katmanı açıyor. Yoğunluk hesabı istemcide değil,
SLD'nin içindeki `<Transformation>` blokunda - GeoServer noktaları çizmeden
önce bir yoğunluk rasterine çeviriyor, `RasterSymbolizer` da onu renklendiriyor.

Yoğunluk her zaman 0-1 arasına ölçekleniyor: 1, o görünümdeki en yoğun bölge
demek, mutlak bir nokta sayısı değil. Ekranın sağ altındaki lejant bu
basamakları gösteriyor; renkleri `mapproject_heatmap.sld` ile aynı tutmak
için `src/map/heatmapScale.js` içinde tekrarlanıyor.

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

## Tarayıcı GeoServer'a doğrudan gitmiyor

WMS görüntüleri de `/api/Map/features` ve `/api/Map/heatmap` uçlarından
geçiyor. Sebebi güvenlik: GeoServer katmanları kimlik sormuyor, dolayısıyla
istemci adresi kendisi kursa `cql_filter`'ı değiştirip başkasının
çizimlerini isteyebilirdi. Filtreyi token'daki kullanıcıya göre sunucu
koyuyor; istemci katman adı bile gönderemiyor.

Ölçülen sonuç - üçü de aynı uca, aynı görünüm için istek attı:

| Kullanıcı | Kayıt sayısı | Resimdeki dolu piksel |
|---|---|---|
| demo | 7 | 3076 |
| admin | 2 | 1683 |
| viewer | 0 | **0** |

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

WMS tarafında da aynı gerekçeyle **1.1.1** kullanıyoruz: 1.3.0'da EPSG:4326
enlem/boylam sayılıyor ve parametrenin adı `srs` değil `crs`.

## Isı haritası tarafındaki iki tuzak

**4. Fonksiyonun adı `vec:Heatmap`, `gs:Heatmap` değil.** Belgelerin ve
örneklerin çoğu `gs:` yazıyor; o ön ek WPS eklentisiyle geliyor ve biz WPS
kurmadık. `HeatmapProcess` sınıfı `org.geotools.process.vector` paketinde
olduğu için `vec:` altında kayıtlı. Yanlış ön ek stili yüklerken
"Unable to find function gs:Heatmap" hatası veriyor.

**5. .NET tarafında parametre adı `request` olmamalı.** OpenLayers her WMS
isteğine `REQUEST=GetMap` ekliyor. Controller'daki parametre `request` diye
adlandırılırsa ASP.NET bunu bir önek sanıp değerleri `request.Bbox`,
`request.Width` adlarıyla arıyor; hiçbirini bulamayınca bütün alanlar
varsayılan kalıyor ve istek sessizce 400'e düşüyor. Parametrenin adı bu
yüzden `viewport`.

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
