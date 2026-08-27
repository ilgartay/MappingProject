#!/usr/bin/env python3
"""
Konum analizi icin ornek POI verisi uretir.

Neden ayri bir betik: bu veri uygulamanin calismasi icin gerekli degil,
analiz ekranini anlamli kilmak icin var. DatabaseInitializer'a koysaydik
her acilista herkesin veritabanina yuzlerce sahte kayit girerdi.

Cikti SQL; calistirmak icin:
    python3 scripts/seed-poi-demo.py | psql -d mapdb

Uretim rastgele ama TOHUMLU: ayni komut her zaman ayni veriyi uretir,
boylece "bende farkli cikti" durumu olusmuyor.

Kategori aramalarinda is_deleted = false sarti var: ayni adda soft delete
edilmis eski bir kategori kalmis olabiliyor ve filtresiz arama onu
yakalayip POI'leri gorunmez bir kategoriye bagliyor.
"""

import random

SEED = 20260824
random.seed(SEED)

# Il merkezleri (boylam, enlem) ve kabaca nufus agirligi.
# Agirlik POI sayisini belirliyor: buyuk sehirde daha cok kayit olsun.
CITIES = [
    ("İstanbul", 28.98, 41.01, 10), ("Ankara", 32.85, 39.93, 8),
    ("İzmir", 27.14, 38.42, 7), ("Bursa", 29.06, 40.18, 5),
    ("Antalya", 30.71, 36.90, 5), ("Adana", 35.33, 37.00, 4),
    ("Konya", 32.48, 37.87, 4), ("Gaziantep", 37.38, 37.07, 4),
    ("Şanlıurfa", 38.80, 37.17, 3), ("Kayseri", 35.48, 38.73, 3),
    ("Mersin", 34.63, 36.81, 3), ("Eskişehir", 30.52, 39.78, 3),
    ("Diyarbakır", 40.24, 37.92, 3), ("Samsun", 36.33, 41.29, 3),
    ("Denizli", 29.09, 37.78, 2), ("Trabzon", 39.72, 41.00, 2),
    ("Erzurum", 41.27, 39.90, 2), ("Malatya", 38.31, 38.35, 2),
    ("Van", 43.38, 38.49, 2), ("Sivas", 37.02, 39.75, 2),
    ("Balıkesir", 27.89, 39.65, 2), ("Manisa", 27.43, 38.62, 2),
    ("Aydın", 27.85, 37.85, 2), ("Muğla", 28.36, 37.22, 2),
    ("Tekirdağ", 27.51, 40.98, 2), ("Kocaeli", 29.92, 40.77, 3),
    ("Hatay", 36.16, 36.20, 2), ("Elazığ", 39.22, 38.68, 1),
    ("Zonguldak", 31.79, 41.45, 1), ("Ordu", 37.88, 40.98, 1),
]

# Kategori: (ad, ust kategori, sehir basina taban adet, isim ornekleri)
#
# Taban adetler bilerek farkli: analiz ekraninda kriterlere farkli agirlik
# verince isi haritasinin degismesi gerekiyor. Her kategori her yerde ayni
# yogunlukta olsaydi agirliklarin etkisi gorunmezdi.
CATEGORIES = [
    ("Restoran", "Yeme-İçme", 6, ["Kebapçı", "Lokanta", "Ocakbaşı", "Balıkçı", "Pide Salonu", "Köfteci"]),
    ("Kafe", "Yeme-İçme", 5, ["Kahve Durağı", "Cafe", "Kıraathane", "Kahvaltıcı"]),
    ("Pastane", "Yeme-İçme", 3, ["Pastanesi", "Fırın", "Tatlıcı"]),
    ("Otel", "Konaklama", 2, ["Otel", "Konak", "Butik Otel"]),
    ("Pansiyon", "Konaklama", 2, ["Pansiyon", "Apart"]),
    ("Eczane", "Sağlık", 4, ["Eczanesi"]),
    ("Hastane", "Sağlık", 1, ["Devlet Hastanesi", "Tıp Merkezi", "Poliklinik"]),
    ("Market", "Alışveriş", 5, ["Market", "Bakkal", "Şarküteri"]),
    ("AVM", "Alışveriş", 1, ["AVM", "Çarşı"]),
    ("Okul", "Eğitim", 4, ["İlkokul", "Lisesi", "Ortaokulu"]),
    ("Üniversite", "Eğitim", 1, ["Üniversitesi", "Fakültesi"]),
    ("spor salonu", "Spor", 2, ["Spor Salonu", "Fitness", "Halı Saha"]),
]

# Kategori bazli sehir egilimi: bir kategorinin belirli sehirlerde yogun
# olmasi analizin anlamli calismasi icin sart. Butun kategoriler her
# sehirde ayni oranda olsaydi, kullanici agirliklari degistirdiginde isi
# haritasi degismezdi - normalize edilmis yogunluk ayni cikardi.
#
# Carpan verilmeyen sehir icin "diger" degeri kullaniliyor.
CATEGORY_BIAS = {
    # Turizm sehirlerinde konaklama yogun
    "Otel":        {"diger": 0.3, "Antalya": 6, "Muğla": 5, "İzmir": 3, "Aydın": 3,
                    "Denizli": 2, "İstanbul": 4, "Trabzon": 2},
    "Pansiyon":    {"diger": 0.3, "Antalya": 5, "Muğla": 6, "Aydın": 3, "Balıkesir": 3,
                    "Ordu": 2, "Trabzon": 2},
    # Universiteler buyuk ve ogrenci sehirlerinde
    "Üniversite":  {"diger": 0.4, "Ankara": 5, "İstanbul": 5, "İzmir": 4,
                    "Eskişehir": 4, "Konya": 3, "Kayseri": 2, "Erzurum": 2},
    # Hastane nufusla olcekleniyor
    "Hastane":     {"diger": 0.6, "İstanbul": 4, "Ankara": 3, "İzmir": 3, "Bursa": 2,
                    "Adana": 2, "Antalya": 2},
    "AVM":         {"diger": 0.3, "İstanbul": 6, "Ankara": 4, "İzmir": 3, "Bursa": 2,
                    "Antalya": 2, "Kocaeli": 2},
    # Sanayi/dogu sehirlerinde daha az kafe, daha cok lokanta
    "Kafe":        {"diger": 0.8, "İstanbul": 3, "İzmir": 3, "Ankara": 2, "Antalya": 2},
    "Restoran":    {"diger": 1.2, "Gaziantep": 4, "Adana": 3, "Hatay": 3, "Şanlıurfa": 2},
    "spor salonu": {"diger": 0.6, "İstanbul": 3, "Ankara": 3, "İzmir": 2, "Kocaeli": 2},
}

NEIGHBOURHOODS = [
    "Merkez", "Yeni", "Cumhuriyet", "Bahçelievler", "Fatih", "Yeşiltepe",
    "Gazi", "Atatürk", "Kültür", "Çamlık", "Yıldız", "Barbaros",
]

# Bicim WorkingHoursPicker'in urettigiyle ayni: demo veri elle
# eklenmis POI'lerden farkli gorunmesin, arama ikisini de bulsun.
HOURS = [
    "Pzt-Cum 09:00-18:00", "Her gün 08:00-20:00", "Her gün 10:00-22:00",
    "7/24", "Pzt-Cum 09:00-17:00", "Her gün 11:00-23:00",
    "Pzt-Cmt 08:30-19:30",
]


def escape(text):
    return text.replace("'", "''")


print("BEGIN;")
print()
print("-- Eksik ust kategoriler (varsa dokunulmuyor)")
for parent in ("Alışveriş", "Eğitim"):
    print(
        f"INSERT INTO poi_category (name, parent_id, created_date, is_deleted, is_active)\n"
        f"SELECT '{escape(parent)}', NULL, now(), false, true\n"
        f"WHERE NOT EXISTS (SELECT 1 FROM poi_category\n"
        f"                  WHERE name = '{escape(parent)}' AND parent_id IS NULL AND is_deleted = false);"
    )

print()
print("-- Eksik alt kategoriler")
for name, parent, _, _ in CATEGORIES:
    print(
        f"INSERT INTO poi_category (name, parent_id, created_date, is_deleted, is_active)\n"
        f"SELECT '{escape(name)}', p.id, now(), false, true\n"
        f"FROM poi_category p\n"
        f"WHERE p.name = '{escape(parent)}' AND p.parent_id IS NULL AND p.is_deleted = false\n"
        f"  AND NOT EXISTS (SELECT 1 FROM poi_category c\n"
        f"                  WHERE c.name = '{escape(name)}' AND c.parent_id = p.id AND c.is_deleted = false);"
    )

print()
print("-- Onceki demo verisi (varsa) temizleniyor: betik tekrar")
print("-- calistirildiginda kayitlar ikiye katlanmasin.")
print("DELETE FROM poi WHERE isim LIKE '%[demo]';")
print()

total = 0
rows = []

for city, lon, lat, weight in CITIES:
    for name, parent, base, samples in CATEGORIES:
        # Sehrin nufus agirligi x kategorinin o sehirdeki egilimi.
        #
        # Bolen yogunlugu belirliyor. Once 2.5 idi, ~1870 POI cikiyordu:
        # harita yakinlasmadan okunamayacak kadar kalabalikti. 10'a
        # cikarinca ~470 kaliyor - isi haritasinin ve agirlikli analizin
        # anlamli olmasi icin yeterli, ekrani bogmuyor.
        bias = CATEGORY_BIAS.get(name, {})
        factor = bias.get(city, bias.get("diger", 1.0))
        count = max(0, int(base * weight * factor / 10) + random.randint(0, 1))

        # 0 cikabilir: o kategori o sehirde hic yok demek, bu da gercekci.
        if count == 0:
            continue

        for _ in range(count):
            # Sehir merkezinin cevresine dagit. 0.12 derece ~ 12 km:
            # buyuk sehirlerde POI'ler genis alana yayilsin, isi haritasi
            # tek bir noktaya yigilmasin.
            spread = 0.03 + 0.012 * weight
            plon = lon + random.gauss(0, spread)
            plat = lat + random.gauss(0, spread * 0.75)

            label = random.choice(samples)
            hood = random.choice(NEIGHBOURHOODS)
            title = f"{hood} {label} [demo]"

            rows.append((escape(title), escape(name), escape(parent),
                         round(plon, 6), round(plat, 6), random.choice(HOURS)))
            total += 1

print(f"-- {total} POI")
print("INSERT INTO poi (isim, kategori_id, mesai_saatleri, geom, user_id, created_date, is_deleted, is_active)")
print("VALUES")

values = []
for title, cat, parent, plon, plat, hours in rows:
    values.append(
        f"  ('{title}',\n"
        f"   (SELECT c.id FROM poi_category c JOIN poi_category p ON p.id = c.parent_id\n"
        f"    WHERE c.name = '{cat}' AND p.name = '{parent}'\n"
        f"      AND c.is_deleted = false AND p.is_deleted = false LIMIT 1),\n"
        f"   '{hours}', ST_Point({plon}, {plat}, 4326),\n"
        f"   (SELECT \"Id\" FROM \"Users\" WHERE \"Username\" = 'admin'), now(), false, true)"
    )

print(",\n".join(values) + ";")
print()
print("COMMIT;")
