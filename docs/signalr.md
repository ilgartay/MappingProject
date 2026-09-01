# SignalR - Canlı Araç Takibi

Simülasyondaki aracın konumu, izleyen istemcilere anlık olarak SignalR
ile gidiyor.

## Neden SignalR

Konum saniyede iki-üç kez değişiyor. İstemci her yarım saniyede bir
"araç nerede" diye sorsaydı (yoklama/polling), on izleyicide dakikada
binlerce boş istek olurdu - çoğu "değişmedi" cevabı dönerdi. SignalR
bağlantıyı açık tutuyor, veri değiştiğinde sunucu kendi haber veriyor.

Alttaki taşıma WebSocket. SignalR tarayıcı desteklemiyorsa uzun
yoklamaya düşebiliyor ama biz `skipNegotiation` ile WebSocket'e
sabitledik: sessizce yavaş bir yola düşmektense bağlantı sorununu
görmeyi tercih ediyoruz.

## Mimari

```
Operatör "Simülasyonu Başlat"
   → POST /api/Transport/routes/{id}/simulation
      → SimulationService: arka planda PeriodicTimer başlar
         → her 400 ms: aracı ilerlet, konumu yayınla
            → ISimulationBroadcaster
               → SignalR grubu "route-{id}"
                  → o hattı takip eden istemciler
```

**Business katmanı SignalR'ı tanımıyor.** `ISimulationBroadcaster`
arayüzü Business'ta, SignalR'lı uygulaması API katmanında. Business
"şunu duyur" diyor; bunun WebSocket'le mi yoksa başka bir yolla mı
yapıldığı onu ilgilendirmiyor. SignalR paketini Business'a eklemek
katman sırasını tersine çevirirdi.

`SimulationService` **tekil (singleton)**: çalışan simülasyonlar
isteklerden bağımsız yaşamalı. Scoped olsaydı operatörün "başlat"
isteği tamamlandığı anda araç da dururdu. Tekil olduğu için `DbContext`
enjekte edemiyor - `IServiceScopeFactory` ile her okumada kendi
kapsamını açıyor (DbContext scoped ve iş parçacığı güvenli değil).

## Gruplar

Her güzergah bir SignalR grubu: `route-1`, `route-2`... "Takip Et"
`JoinRoute(id)`, "Takibi Bırak" `LeaveRoute(id)` çağırıyor.

Grup kullanmasaydık her aracın konumu bağlı olan herkese giderdi;
on hattın çalıştığı bir sistemde herkes izlemediği dokuz aracın verisini
de indirirdi.

## Kimlik doğrulama

Hub `[Authorize]` ile korunuyor - token'sız istek 401.

**Tuzak:** tarayıcının WebSocket API'si el sıkışmaya `Authorization`
başlığı eklemeye izin vermiyor. SignalR bu yüzden token'ı
`?access_token=...` sorgu parametresiyle gönderiyor. `Program.cs` bunu
yalnızca `/hubs` altındaki yollar için kabul ediyor:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context => { /* access_token -> context.Token */ }
};
```

CORS politikasına `AllowCredentials()` eklendi; onsuz tarayıcı
WebSocket yükseltmesini engelliyor.

## Tuzaklar

- **camelCase.** MVC controller'ları JSON'u camelCase üretiyor, SignalR
  varsayılanda üretmiyor - C#'taki adı aynen gönderiyor. Aynı nesne
  REST'ten `routeId`, SignalR'dan `RouteId` diye gelirdi.
  `AddJsonProtocol` ile ikisi eşitlendi.
- **StrictMode.** React geliştirmede efekti iki kez çalıştırıyor; ilk
  bağlantı `start()` bitmeden temizleniyor ve "Failed to start the
  HttpConnection before stop() was called" hatası düşüyor. Kapatma
  `start()` sonuçlanana kadar erteleniyor.
- **Takip bırakılınca veri donuyor.** Yayını almayı bırakan istemcinin
  elindeki son yüzde olduğu yerde kalıyor. Donmuş sayıyı canlıymış gibi
  göstermemek için yüzde yalnızca takipteyken gösteriliyor, ve
  `GET /api/Transport/simulations` on saniyede bir yoklanarak "bu hatta
  araç var mı" bilgisi taze tutuluyor.
- **Sayfayı sonradan açan.** Yalnızca SignalR dinleseydi, simülasyon
  başladıktan sonra giren kullanıcı hiçbir şey göremezdi. Açılışta
  `/simulations` bir kez okunuyor.

## Simülasyonun kendisi

- Araç `guzergah.rota_geom` üzerinde ilerliyor; konum, parça uzunlukları
  haversine ile metre cinsinden hesaplanarak bulunuyor (`RoutePath`).
  Derece cinsinden uzunlukla ilerleseydik araç doğu-batı giderken
  hızlanır, kuzey-güney giderken yavaşlardı.
- Süre OSRM'in verdiği gerçek süreden türüyor, `SpeedFactor` (20) ile
  bölünüp 20-180 saniye arasına kırpılıyor. 17 dakikalık bir hattı
  gerçek zamanda izletmek sunumu imkansız kılardı.
- Yön (`heading`) 25 metre ileriye bakılarak hesaplanıyor: ardışık iki
  koordinat çok yakın olabiliyor ve aradaki açı gürültülü çıkıyor.
