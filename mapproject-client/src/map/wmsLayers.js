import ImageLayer from 'ol/layer/Image'
import ImageWMS from 'ol/source/ImageWMS'

import { API_BASE_URL } from '../api/client'
import { getToken } from '../auth/authStorage'

/**
 * WMS görüntüsünü token'la indirir.
 *
 * OpenLayers varsayılan olarak <img src="..."> kuruyor; <img> etiketine
 * Authorization başlığı eklenemediği için isteğimiz 401 dönerdi. Bunun
 * yerine fetch ile indirip blob URL veriyoruz.
 */
function loadImageWithToken(image, src) {
  const element = image.getImage()

  fetch(src, { headers: { Authorization: `Bearer ${getToken()}` } })
    .then((response) => (response.ok ? response.blob() : Promise.reject(response.status)))
    .then((blob) => {
      const objectUrl = URL.createObjectURL(blob)

      // Blob URL'leri tarayıcı kendiliğinden temizlemiyor; resim
      // yüklendikten sonra bırakmazsak her pan/zoom bellek sızdırır.
      element.addEventListener('load', () => URL.revokeObjectURL(objectUrl), { once: true })
      element.src = objectUrl
    })
    .catch(() => {
      // Boş bırakıyoruz: OpenLayers resmi "hatalı" sayıp bir sonraki
      // görünümde yeniden deniyor. Hata mesajını MapView gösteriyor.
      element.src = ''
    })
}

/**
 * Sunucuda çizilmiş bir WMS katmanı üretir.
 *
 * Adres bizim API'miz, GeoServer değil: katman adını ve kullanıcı
 * filtresini sunucu koyuyor. LAYERS parametresini OpenLayers zorunlu
 * tuttuğu için gönderiyoruz ama sunucu onu yok sayıyor.
 *
 * @param {string} path '/api/Map/features' | '/api/Map/heatmap'
 */
function createWmsLayer(path, options = {}) {
  const source = new ImageWMS({
    url: `${API_BASE_URL}${path}`,
    params: { LAYERS: 'mapproject' },
    imageLoadFunction: loadImageWithToken,
    // Görünenden biraz büyük istiyoruz: küçük kaydırmalarda yeni istek
    // atılmasın, harita takılmasın.
    ratio: 1.2,
  })

  return new ImageLayer({ source, ...options })
}

/** Çizimlerin genel gösterimi (nokta + çizgi + poligon, kendi renkleriyle). */
export function createFeatureWmsLayer() {
  return createWmsLayer('/api/Map/features')
}

/** Nokta yoğunluğundan üretilen ısı haritası; açılana kadar gizli. */
export function createHeatmapLayer() {
  return createWmsLayer('/api/Map/heatmap', { visible: false })
}

/**
 * Katmanı sunucudan yeniden ister. Çizim eklendiğinde/silindiğinde
 * çağrılıyor: WMS bir resim olduğu için kendiliğinden güncellenmiyor.
 *
 * @param {() => void} [onSettled]
 *   Yeni resim ekrana geldiğinde (ya da yüklenemediğinde) çalışır.
 *   Yeni kaydı resim gelene kadar görünür tutmak için kullanılıyor.
 *   Hata durumunu da dinliyoruz: yalnızca başarıyı beklersek, istek
 *   patladığında geri çağrı hiç çalışmaz ve kayıt sonsuza dek
 *   "geçici görünür" halde kalırdı.
 */
export function refreshWmsLayer(layer, onSettled) {
  const source = layer?.getSource()
  if (!source) return

  if (onSettled) {
    source.once('imageloadend', onSettled)
    source.once('imageloaderror', onSettled)
  }

  source.refresh()
}
