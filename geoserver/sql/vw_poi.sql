-- POI katmaninin GeoServer SQL View'i.
--
-- Diger view'lardan farki: kategori agacini ozyinelemeli CTE ile gezip
-- "Yeme-Icme > Restoran" biciminde tam yolu uretiyor ve ekleyen
-- kullanicinin adini da join'liyor. Boylece liste tek WFS istegiyle
-- tam geliyor; API'nin ayrica kategori ve kullanici sorgusu atmasi
-- gerekmiyor.
--
-- Silinmis POI ve silinmis kategori burada eleniyor - kural view'in
-- icinde durdugu icin WFS ve WMS ayni sonucu goruyor.
WITH RECURSIVE cat_path AS (
    SELECT id, name, parent_id, name::text AS path
    FROM poi_category
    WHERE parent_id IS NULL AND is_deleted = false
  UNION ALL
    SELECT c.id, c.name, c.parent_id, cp.path || ' → ' || c.name
    FROM poi_category c
    JOIN cat_path cp ON c.parent_id = cp.id
    WHERE c.is_deleted = false
)
SELECT p.id,
       p.isim,
       p.kategori_id,
       cp.name AS kategori_adi,
       cp.path AS kategori_yolu,
       p.mesai_saatleri,
       p.geom,
       p.user_id,
       u."Username" AS kullanici,
       p.created_date,
       p.modified_date,
       p.is_active
FROM poi p
JOIN cat_path cp ON cp.id = p.kategori_id
JOIN "Users" u ON u."Id" = p.user_id
WHERE p.is_deleted = false
