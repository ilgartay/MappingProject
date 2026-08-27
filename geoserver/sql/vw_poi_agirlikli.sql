-- Agirlikli POI katmani: konum analizinin isi haritasini besliyor.
--
-- Kullanici analiz ekraninda 2-5 kategori secip her birine 100 uzerinden
-- puan veriyor. O puanlar bu view'a PARAMETRE olarak geliyor (%k1%, %a1%
-- ...), her POI kendi kategorisinin puanini "agirlik" kolonunda tasiyor.
-- Isi haritasi SLD'si de weightAttr=agirlik diyerek noktalari bu puana
-- gore agirlikliyor: puani yuksek kategori haritayi daha cok isitiyor.
--
-- Parametreler tamsayi olarak dogrulaniyor (GeoServer'da regexpValidator),
-- bu yuzden SQL'e disaridan ifade sizmasi mumkun degil.
--
-- Secilmeyen kategoriler 0 agirlik aliyor; istek cql_filter ile
-- "agirlik > 0" diyerek onlari zaten disarida birakiyor.
SELECT p.id,
       p.isim,
       p.kategori_id,
       c.name AS kategori_adi,
       p.geom,
       CASE p.kategori_id
         WHEN %k1%::int THEN %a1%::int
         WHEN %k2%::int THEN %a2%::int
         WHEN %k3%::int THEN %a3%::int
         WHEN %k4%::int THEN %a4%::int
         WHEN %k5%::int THEN %a5%::int
         ELSE 0
       END AS agirlik
FROM poi p
JOIN poi_category c ON c.id = p.kategori_id
WHERE p.is_deleted = false AND c.is_deleted = false AND p.is_active = true
