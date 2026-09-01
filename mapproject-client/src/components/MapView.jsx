import { useCallback, useEffect, useRef, useState } from 'react'
import Draw from 'ol/interaction/Draw'
import Modify from 'ol/interaction/Modify'
import Collection from 'ol/Collection'
import Feature from 'ol/Feature'
import LineString from 'ol/geom/LineString'
import Point from 'ol/geom/Point'
import { fromLonLat } from 'ol/proj'

import {
  ENDPOINT_BY_GEOMETRY,
  createFeature,
  deleteFeature,
  fetchFeatures,
  updateFeature,
} from '../api/features'
import { analyzeIntersection } from '../api/analysis'
import { createPoi, deletePoi, fetchCategories, fetchPois } from '../api/poi'
import { criteriaToParam, fetchProvince, fetchProvinces, validateAnalysis } from '../api/location'
import {
  buildRoute,
  createRoute,
  createStop,
  deleteRoute,
  deleteStop,
  fetchRoutes,
  reorderStops,
  updateRoute,
} from '../api/transport'
import {
  createSimulationConnection,
  fetchSimulations,
  startSimulation,
  stopSimulation,
} from '../api/simulation'
import { useAuth } from '../auth/useAuth'
import { useMapInstance } from '../map/useMapInstance'
import { geometryToWkt, wktToFeature } from '../map/wkt'
import { refreshWmsLayer } from '../map/wmsLayers'
import AnalysisPanel from './AnalysisPanel'
import HeatmapLegend from './HeatmapLegend'
import LocationAnalysisPanel from './LocationAnalysisPanel'
import RoutePanel from './RoutePanel'
import SaveStopDialog from './SaveStopDialog'
import StopInfoPanel from './StopInfoPanel'
import VehicleInfoPanel from './VehicleInfoPanel'
import DeletePoiDialog from './DeletePoiDialog'
import PoiInfoPanel from './PoiInfoPanel'
import PoiSearch from './PoiSearch'
import SavePoiDialog from './SavePoiDialog'
import FeatureDetailPanel from './FeatureDetailPanel'
import CoordinateSearch from './CoordinateSearch'
import DrawToolbar from './DrawToolbar'
import SaveFeatureDialog from './SaveFeatureDialog'
import DeleteFeatureDialog from './DeleteFeatureDialog'
import './MapView.css'

/**
 * Feature id'lerini "points-3" biçiminde kuruyoruz; buradan hem silme
 * uç noktasını hem de veritabanı id'sini geri çıkarıyoruz.
 * Henüz kaydedilmemiş çizimlerin id'si olmaz, o durumda null döner.
 */
function parseFeatureId(feature) {
  const raw = feature.getId()
  if (!raw) return null

  const [group, id] = String(raw).split('-')
  return {
    countKey: group, // 'points' | 'lines' | 'polygons'
    endpoint: group.slice(0, -1), // 'point' | 'line' | 'polygon'
    id: Number(id),
  }
}

export default function MapView() {
  const { hasPermission, allowedAreaWkt } = useAuth()
  const canUpdate = hasPermission('feature.update')

  const containerRef = useRef(null)
  const drawRef = useRef(null)

  // Harita kurulumu ve katmanlar ayrı hook'ta; burada sadece kullanıyoruz.
  const {
    mapRef,
    sourceRef,
    featureLayerRef,
    targetSourceRef,
    analysisSourceRef,
    areaSourceRef,
    featureWmsRef,
    heatmapRef,
    locationAnalysisRef,
    locationAreaSourceRef,
    routeLineSourceRef,
    osrmRouteSourceRef,
    stopSourceRef,
    stopLayerRef,
    vehicleSourceRef,
    vehicleLayerRef,
    poiWmsRef,
    poiSourceRef,
    poiLayerRef,
  } = useMapInstance(containerRef)

  // 'Point' | 'LineString' | 'Polygon' | 'Analysis'
  const [activeTool, setActiveTool] = useState(null)
  const [pending, setPending] = useState(null) // kaydedilmeyi bekleyen çizim
  const [selected, setSelected] = useState(null) // detayı açık olan çizim
  const [isConfirmingDelete, setIsConfirmingDelete] = useState(false)
  const [analysis, setAnalysis] = useState(null) // kesişim analizi sonucu
  const [counts, setCounts] = useState({ points: 0, lines: 0, polygons: 0 })
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isHeatmapOn, setIsHeatmapOn] = useState(false)

  // POI durumu: kategoriler (form için), kaydedilmeyi bekleyen çizim,
  // bilgi paneli açık olan kayıt.
  const [categories, setCategories] = useState([])
  // Arama barı bu listenin üstünde çalışıyor; POI'ler zaten tek istekte
  // geldiği için ayrıca sunucuya sormaya gerek yok.
  const [pois, setPois] = useState([])

  // --- Konum analizi ---
  const [isLocationOpen, setIsLocationOpen] = useState(false)
  const [provinces, setProvinces] = useState([])
  const [provinceId, setProvinceId] = useState(null)
  const [areaWkt, setAreaWkt] = useState(null)
  const [criteria, setCriteria] = useState([
    { categoryId: '', weight: 50 },
    { categoryId: '', weight: 50 },
  ])
  const [isAnalysisOn, setIsAnalysisOn] = useState(false)
  const [locationError, setLocationError] = useState('')

  // --- Ulaşım modülü ---
  const [isRoutePanelOpen, setIsRoutePanelOpen] = useState(false)
  const [routes, setRoutes] = useState([])
  const [selectedRouteId, setSelectedRouteId] = useState(null)
  const [pendingStop, setPendingStop] = useState(null)
  const [selectedStop, setSelectedStop] = useState(null)
  const [transportError, setTransportError] = useState('')
  const [isBuildingRoute, setIsBuildingRoute] = useState(false)

  // --- Simülasyon ---
  //
  // simulations: güzergah id -> aracın son bilinen durumu.
  // trackedRouteId: aynı anda tek hat takip ediliyor. Birden çoğuna izin
  // verseydik kamera hangi aracın peşinden gideceğini bilemezdi.
  const [simulations, setSimulations] = useState({})
  const [trackedRouteId, setTrackedRouteId] = useState(null)
  const [isSimulationBusy, setIsSimulationBusy] = useState(false)
  // Açık bilgi kutusu, id olarak tutuluyor: nesneyi saklasaydık yüzde
  // ilerledikçe kutudaki değer donup kalırdı.
  const [vehicleRouteId, setVehicleRouteId] = useState(null)
  // Sefer bitince araç ve bilgi kutusu bir anda kayboluyor; kısa bir not
  // bırakmazsak kullanıcı ne olduğunu anlamıyor.
  const [simulationNotice, setSimulationNotice] = useState('')
  const noticeTimerRef = useRef(null)
  const connectionRef = useRef(null)

  // Araç konumu saniyede 2,5 kez geliyor. Hepsini React durumuna
  // yazsaydık MapView de saniyede 2,5 kez (StrictMode ile 5) yeniden
  // render edilirdi - ölçtük, harita gözle görülür şekilde takılıyordu.
  //
  // Bu yüzden canlı veri ref'te duruyor ve haritayı doğrudan güncelliyor;
  // React durumu yalnızca panel ve bilgi kutusundaki metin için, saniyede
  // bir tazeleniyor. Harita akıcı kalıyor, ağaç boşuna diff'lenmiyor.
  const vehiclesRef = useRef({})
  const lastPublishRef = useRef(0)
  // trackedRouteId'nin aynası: SignalR geri çağrısı bir kez kurulduğu
  // için state'in güncel değerini göremez, ref görür.
  const trackedRouteIdRef = useRef(null)

  // Katman kontrolü: haritada gizlenen güzergahların id'leri.
  // Gizlemek "yok saymak" değil; kayıt yerinde duruyor, yalnızca
  // çizilmiyor - listede de görünmeye devam ediyor.
  const [hiddenRouteIds, setHiddenRouteIds] = useState(() => new Set())
  const [pendingPoi, setPendingPoi] = useState(null)
  const [selectedPoi, setSelectedPoi] = useState(null)
  const [isConfirmingPoiDelete, setIsConfirmingPoiDelete] = useState(false)

  /**
   * Poligonu backend'e gönderip kesişen envanterleri sayar.
   * Hem kaydedilen poligon hem geçici analiz aracı bunu kullanıyor.
   * @param {string} wkt EPSG:4326 poligon
   * @param {string} title panelde gösterilecek başlık
   * @param {number} [excludePolygonId] poligonun kendisini saymamak için
   */
  const runAnalysis = useCallback(async (wkt, title, excludePolygonId) => {
    setAnalysis({ title, isLoading: true })

    try {
      const result = await analyzeIntersection(wkt, excludePolygonId)
      setAnalysis({ title, result })
    } catch (err) {
      // 401 ise interceptor zaten oturumu kapatıyor, panel göstermeye gerek yok.
      if (err.response?.status === 401) {
        setAnalysis(null)
        return
      }
      setAnalysis({ title, error: err.response?.data?.message ?? 'Analiz yapılamadı.' })
    }
  }, [])

  const clearAnalysis = useCallback(() => {
    analysisSourceRef.current?.clear()
    setAnalysis(null)
  }, [analysisSourceRef])

  // --- Tanımlı çizim alanını haritada göster ---
  useEffect(() => {
    const source = areaSourceRef.current
    if (!source) return

    source.clear()

    // null = kısıt yok, çizecek sınır de yok.
    if (allowedAreaWkt) {
      source.addFeature(wktToFeature(allowedAreaWkt))
    }
  }, [allowedAreaWkt, areaSourceRef])

  // --- Kayıtlı geometrileri yükle ---
  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const data = await fetchFeatures()
        if (cancelled) return

        const source = sourceRef.current
        source.clear()

        for (const [key, group] of Object.entries(data)) {
          for (const item of group) {
            // WKT 4326 -> harita 3857 dönüşümü burada oluyor.
            const feature = wktToFeature(item.wkt)
            feature.set('name', item.name)
            feature.set('color', item.color)
            feature.setId(`${key}-${item.id}`)
            source.addFeature(feature)
          }
        }

        setCounts({
          points: data.points.length,
          lines: data.lines.length,
          polygons: data.polygons.length,
        })
      } catch (err) {
        // 401 ise interceptor zaten oturumu kapatıp login'e atıyor.
        if (!cancelled && err.response?.status !== 401) {
          setError('Kayıtlı çizimler yüklenemedi.')
        }
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }

    load()
    return () => {
      cancelled = true
    }
  }, [sourceRef])

  // --- POI'leri ve kategorileri yükle ---
  useEffect(() => {
    let cancelled = false

    Promise.all([fetchPois(), fetchCategories()])
      .then(([pois, categoryList]) => {
        if (cancelled) return

        const source = poiSourceRef.current
        source.clear()

        for (const poi of pois) {
          const feature = wktToFeature(poi.wkt)
          feature.setId(`poi-${poi.id}`)
          feature.set('name', poi.name)
          // Bilgi panelinin göstereceği her şey feature'da dursun:
          // panel açılırken sunucuya ikinci bir istek gerekmesin.
          feature.set('poi', poi)
          source.addFeature(feature)
        }

        setPois(pois)
        setCategories(categoryList)
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setError('POI kayıtları yüklenemedi.')
        }
      })

    return () => {
      cancelled = true
    }
  }, [poiSourceRef])

  /**
   * Güzergahları haritaya çizer: her durak kendi hattının renginde ve
   * sıra numarasıyla, duraklar da sırayla bir çizgiyle birleştirilmiş.
   *
   * Kaynakları her seferinde baştan kuruyoruz. Tek tek eklemek/çıkarmak
   * yerine bu, çünkü sıralama değişince zaten hepsinin yeniden çizilmesi
   * gerekiyor - ayrıca "hangisi değişti" hesabı tutmak gereksiz.
   */
  const drawRoutes = useCallback(
    (routeList, hidden) => {
      const stopSource = stopSourceRef.current
      const lineSource = routeLineSourceRef.current
      const osrmSource = osrmRouteSourceRef.current
      if (!stopSource || !lineSource || !osrmSource) return

      stopSource.clear()
      lineSource.clear()
      osrmSource.clear()

      for (const route of routeList) {
        // Kapatılan güzergah hiç çizilmiyor: durakları da, rotası da.
        if (hidden.has(route.id)) continue

        const coordinates = []

        for (const stop of route.stops) {
          const feature = wktToFeature(stop.wkt)
          feature.setId(`stop-${stop.id}`)
          feature.set('order', stop.order)
          feature.set('routeColor', route.color)
          // Bilgi kutusunun göstereceği her şey feature'da dursun:
          // tıklanınca sunucuya ikinci bir istek gerekmesin.
          feature.set('stop', stop)
          stopSource.addFeature(feature)

          coordinates.push(feature.getGeometry().getCoordinates())
        }

        // OSRM rotası varsa asıl yol o; kesikli yardımcı hat yalnızca
        // rota üretilmemişken durak sırasını göstermek için çiziliyor.
        if (route.routeWkt) {
          const osrmLine = wktToFeature(route.routeWkt)
          osrmLine.set('routeColor', route.color)
          osrmSource.addFeature(osrmLine)
        } else if (coordinates.length > 1) {
          const line = new Feature(new LineString(coordinates))
          line.set('routeColor', route.color)
          lineSource.addFeature(line)
        }
      }
    },
    [stopSourceRef, routeLineSourceRef, osrmRouteSourceRef],
  )

  const loadRoutes = useCallback(() => {
    return fetchRoutes().then((list) => {
      setRoutes(list)
      return list
    })
  }, [])

  // Harita, güzergah listesinin ve katman anahtarlarının aynası. Çizimi
  // tek bir yerden yapmak, "listeyi güncelledim ama haritayı unuttum"
  // durumunu baştan imkansız kılıyor.
  useEffect(() => {
    drawRoutes(routes, hiddenRouteIds)
  }, [routes, hiddenRouteIds, drawRoutes])

  /** Aracı haritaya çizer. React'ten bağımsız: doğrudan OpenLayers kaynağı. */
  const drawVehicle = useCallback(
    (state) => {
      const source = vehicleSourceRef.current
      if (!source) return

      source.clear()
      if (!state) return

      const feature = new Feature(new Point(fromLonLat([state.longitude, state.latitude])))
      feature.set('routeColor', state.routeColor)
      feature.set('heading', state.heading)
      feature.set('routeId', state.routeId)
      source.addFeature(feature)
    },
    [vehicleSourceRef],
  )

  /**
   * Kamerayı aracın peşinde tutar.
   *
   * Her yayında ortalasaydık kullanıcı haritayı kaydıramaz, sürekli geri
   * çekilirdi. Bunun yerine araç kenara yaklaşınca bir kez ortalıyoruz.
   */
  const followVehicle = useCallback(
    (state) => {
      const map = mapRef.current
      if (!map || !state) return

      const center = fromLonLat([state.longitude, state.latitude])
      const [minX, minY, maxX, maxY] = map.getView().calculateExtent(map.getSize())
      const marginX = (maxX - minX) * 0.2
      const marginY = (maxY - minY) * 0.2

      const isComfortablyInside =
        center[0] > minX + marginX &&
        center[0] < maxX - marginX &&
        center[1] > minY + marginY &&
        center[1] < maxY - marginY

      if (!isComfortablyInside) {
        map.getView().animate({ center, duration: 500 })
      }
    },
    [mapRef],
  )

  // --- Canlı yayın bağlantısı ---
  //
  // Bağlantı bir kez kuruluyor ve oturum boyunca açık kalıyor. Takip
  // edilen hat değişince yeni bağlantı açmıyoruz, sadece SignalR
  // grubunu değiştiriyoruz.
  useEffect(() => {
    const connection = createSimulationConnection()
    connectionRef.current = connection

    connection.on('VehicleMoved', (state) => {
      // Araç son durağa vardı: son yayını aldık, kaydı düşürüyoruz.
      //
      // Bildirim setSimulations'ın dışında: durum güncelleyicinin içinde
      // yan etki yapmak StrictMode'da iki kez çalışır.
      if (state.isFinished) {
        setSimulationNotice(`${state.routeName}: araç son durağa vardı.`)
        clearTimeout(noticeTimerRef.current)
        noticeTimerRef.current = setTimeout(() => setSimulationNotice(''), 6000)

        delete vehiclesRef.current[state.routeId]

        if (trackedRouteIdRef.current === state.routeId) drawVehicle(null)

        setSimulations((current) => {
          const next = { ...current }
          delete next[state.routeId]
          return next
        })
        return
      }

      vehiclesRef.current[state.routeId] = state

      // Harita her yayında güncelleniyor: akıcılık buradan geliyor.
      if (trackedRouteIdRef.current === state.routeId) {
        drawVehicle(state)
        followVehicle(state)
      }

      // Metin saniyede bir: panelde ve bilgi kutusunda yüzdenin 400 ms'de
      // bir değişmesi kimseye bir şey katmıyor, render maliyeti ise gerçek.
      const now = performance.now()

      if (now - lastPublishRef.current >= 1000) {
        lastPublishRef.current = now
        setSimulations({ ...vehiclesRef.current })
      }
    })

    // StrictMode geliştirmede efekti iki kez çalıştırıyor: ilk bağlantı
    // daha start() bitmeden temizleniyor. Bekleyen bir start()'ın
    // üstüne stop() çağırmak "Failed to start the HttpConnection before
    // stop() was called" hatası veriyor, o yüzden kapatmayı start()
    // sonuçlanana kadar erteliyoruz.
    let isDisposed = false

    connection
      .start()
      .then(() => {
        if (isDisposed) connection.stop()
      })
      .catch(() => {
        if (!isDisposed) setTransportError('Canlı takip bağlantısı kurulamadı.')
      })

    return () => {
      isDisposed = true
      connectionRef.current = null
      clearTimeout(noticeTimerRef.current)

      // Bağlantı kurulduysa hemen kapat; hâlâ kuruluyorsa yukarıdaki
      // then bloğu kapatacak.
      if (connection.state === 'Connected') connection.stop()
    }
    // drawVehicle ve followVehicle yalnızca ref'lere bağlı, kimlikleri
    // sabit: listede olmaları bağlantıyı yeniden kurdurmuyor.
  }, [drawVehicle, followVehicle])

  // Sayfa açıldığında çalışan simülasyonları bir kez soruyoruz: yalnızca
  // SignalR dinleseydik, simülasyon başladıktan sonra giren kullanıcı
  // hangi hatlarda araç olduğunu hiç bilemezdi.
  useEffect(() => {
    let cancelled = false

    fetchSimulations()
      .then((list) => {
        if (cancelled) return
        setSimulations(Object.fromEntries(list.map((item) => [item.routeId, item])))
      })
      .catch(() => {
        // Sessiz: simülasyon listesi uygulamanın çalışması için şart değil.
      })

    return () => {
      cancelled = true
    }
  }, [])

  // Çalışan simülasyon listesini aralıklarla tazeliyoruz.
  //
  // Takibi bırakan istemci o hattın yayınını almıyor; sefer bittiğinde
  // "bitti" mesajı da gelmiyor ve panel sonsuza kadar "araç yolda"
  // derdi. Konum değil, yalnızca hangi hatlarda araç olduğu bilgisi
  // buradan tazeleniyor - takip edilen hattın verisi SignalR'dan geldiği
  // için daha taze, ona dokunmuyoruz.
  useEffect(() => {
    const interval = setInterval(() => {
      fetchSimulations()
        .then((list) => {
          setSimulations((current) => {
            const next = Object.fromEntries(list.map((item) => [item.routeId, item]))

            if (trackedRouteId !== null && current[trackedRouteId]) {
              next[trackedRouteId] = current[trackedRouteId]
            }

            // Aynı hatlar koşuyorsa yeni nesne üretmiyoruz: her on
            // saniyede bir boşuna render tetiklemenin anlamı yok.
            const unchanged =
              Object.keys(next).length === Object.keys(current).length &&
              Object.keys(next).every((id) => id in current)

            return unchanged ? current : next
          })
        })
        .catch(() => {
          // Sessiz: bir sonraki turda tekrar denenecek.
        })
    }, 10000)

    return () => clearInterval(interval)
  }, [trackedRouteId])

  // Takip edilen hat değişince aracı hemen çiziyoruz: bir sonraki
  // yayını beklemek "Takip Et"e bastıktan sonra yarım saniye boş harita
  // demek olurdu.
  useEffect(() => {
    trackedRouteIdRef.current = trackedRouteId
    drawVehicle(trackedRouteId === null ? null : vehiclesRef.current[trackedRouteId] ?? null)
  }, [trackedRouteId, drawVehicle])

  // --- Güzergahları yükle ---
  useEffect(() => {
    let cancelled = false

    fetchRoutes()
      .then((list) => {
        if (cancelled) return
        setRoutes(list)
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setTransportError('Güzergahlar yüklenemedi.')
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  // --- İl listesini yükle ---
  useEffect(() => {
    let cancelled = false

    fetchProvinces()
      .then((list) => {
        if (!cancelled) setProvinces(list)
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setLocationError('İl listesi yüklenemedi.')
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  // --- Seçilen ilin sınırını haritada göster ---
  useEffect(() => {
    const source = locationAreaSourceRef.current
    if (!source) return

    // İl seçimi haritaya çizilen alanın yerini alıyor: hedef bölge tek.
    if (provinceId === null) return

    let cancelled = false

    fetchProvince(provinceId)
      .then((province) => {
        if (cancelled || !province?.wkt) return

        source.clear()
        source.addFeature(wktToFeature(province.wkt))
        setAreaWkt(null)
      })
      .catch((err) => {
        if (!cancelled && err.response?.status !== 401) {
          setLocationError('İl sınırı okunamadı.')
        }
      })

    return () => {
      cancelled = true
    }
  }, [provinceId, locationAreaSourceRef])

  // --- Seçili araca göre Draw etkileşimini kur ---
  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    // Önceki aracı her durumda kaldır: iki etkileşim aynı anda açık kalırsa
    // tek tıklama iki geometri çizmeye başlar.
    if (drawRef.current) {
      map.removeInteraction(drawRef.current)
      drawRef.current = null
    }

    // Kaydet penceresi açıkken yeni çizim başlatılmasın.
    if (!activeTool || pending || pendingPoi) return

    // Analiz aracı da poligon çizer, ama başka katmana ve kaydetmeden.
    const isAnalysis = activeTool === 'Analysis'
    // POI de nokta çizer, ama kendi katmanına ve kendi formuyla.
    const isPoi = activeTool === 'Poi'
    // Konum analizinin hedef bölgesi de poligon; kendi katmanına gidiyor.
    const isLocationArea = activeTool === 'LocationArea'
    // Durak nokta çiziyor ama hiçbir kaynağa eklenmiyor: kayıt bitince
    // zaten güzergahlar sunucudan tazelenip haritaya yeniden çiziliyor.
    const isStop = activeTool === 'Stop'

    const targetSource = isAnalysis
      ? analysisSourceRef.current
      : isPoi
        ? poiSourceRef.current
        : isLocationArea
          ? locationAreaSourceRef.current
          : sourceRef.current

    const draw = isStop
      ? new Draw({ type: 'Point' })
      : new Draw({
          source: targetSource,
          type: isAnalysis || isLocationArea ? 'Polygon' : isPoi ? 'Point' : activeTool,
        })

    if (isLocationArea) {
      // Tek hedef bölge: yeni çizim eskisinin yerini alsın.
      draw.on('drawstart', () => locationAreaSourceRef.current.clear())
    }

    draw.on('drawend', (event) => {
      const wkt = geometryToWkt(event.feature.getGeometry())

      if (isStop) {
        setPendingStop({ wkt })
        setActiveTool(null)
        return
      }

      if (isLocationArea) {
        // İl seçimiyle çizim birbirinin yerini alıyor; hedef bölge tek.
        setProvinceId(null)
        setAreaWkt(wkt)
        setActiveTool(null)
        return
      }

      if (isAnalysis) {
        // Draw, yeni feature'ı bu olaydan SONRA source'a ekliyor;
        // o yüzden burada clear() demek sadece önceki analizi siler.
        analysisSourceRef.current.clear()

        // Aracı kapatıyoruz: sonuç paneli okunurken yanlışlıkla
        // yeni bir poligona başlanmasın.
        setActiveTool(null)
        runAnalysis(wkt, 'Envanter analizi')
        return
      }

      if (isPoi) {
        // POI kendi katmanında; WMS'le ilgisi yok, o yüzden 'interactive'
        // işaretine de gerek yok - poiStyle zaten görünür çiziyor.
        setPendingPoi({ feature: event.feature })
        setActiveTool(null)
        return
      }

      // Feature'ı source'a Draw kendisi ekliyor; biz sadece bilgilerini
      // sormak için bekletiyoruz. İptal edilirse geri çıkaracağız.
      //
      // Görünür işaretini burada koyuyoruz: kullanıcı isim/renk penceresini
      // doldururken çizdiği şekli görmeye devam etsin. İşaret kaydetme
      // bitip WMS resmi gelene kadar duruyor (handleSave).
      event.feature.set('interactive', true)
      setPending({ feature: event.feature, geometryType: activeTool })
    })

    map.addInteraction(draw)
    drawRef.current = draw

    return () => {
      map.removeInteraction(draw)
      drawRef.current = null
    }
  }, [
    activeTool,
    pending,
    pendingPoi,
    runAnalysis,
    mapRef,
    sourceRef,
    analysisSourceRef,
    poiSourceRef,
    locationAreaSourceRef,
  ])

  // --- Çizime tıklayınca silme penceresini aç ---
  useEffect(() => {
    const map = mapRef.current
    if (!map) return

    // Çizim yapılırken veya bir pencere açıkken seçim devre dışı;
    // yoksa çizim tıklaması aynı anda silme penceresini de açardı.
    if (activeTool || pending || pendingPoi || selected || selectedPoi || selectedStop) return

    function onClick(event) {
      // Araç en üstteki katman; hareket ettiği için ona basmak zaten zor,
      // altındaki bir durağı seçmek büsbütün imkansız kılardı.
      const vehicleFeature = map.forEachFeatureAtPixel(event.pixel, (f) => f, {
        layerFilter: (layer) => layer === vehicleLayerRef.current,
      })

      if (vehicleFeature) {
        setVehicleRouteId(vehicleFeature.get('routeId'))
        return
      }

      // Duraklara önce bakıyoruz: durak işaretleri POI ve çizimlerin
      // üstünde duruyor, alttakini seçmek üstündekini tıklanamaz yapardı.
      const stopFeature = map.forEachFeatureAtPixel(event.pixel, (f) => f, {
        layerFilter: (layer) => layer === stopLayerRef.current,
      })

      if (stopFeature) {
        setSelectedStop(stopFeature.get('stop'))
        return
      }

      // POI'ye önce bakıyoruz: POI işareti çizimin üstünde duruyor, alttaki
      // çizimi seçmek üstündekini tıklanamaz yapardı.
      const poiFeature = map.forEachFeatureAtPixel(event.pixel, (f) => f, {
        layerFilter: (layer) => layer === poiLayerRef.current,
      })

      if (poiFeature) {
        setSelectedPoi(poiFeature.get('poi'))
        return
      }

      // layerFilter: sadece çizim katmanına bak. Olmasaydı arama işaretine
      // tıklamak üstündeki çizimi bulmayı engellerdi.
      const feature = map.forEachFeatureAtPixel(event.pixel, (f) => f, {
        layerFilter: (layer) => layer === featureLayerRef.current,
      })
      if (!feature) return

      const parsed = parseFeatureId(feature)
      if (!parsed) return

      setSelected({
        ...parsed,
        feature,
        name: feature.get('name'),
        color: feature.get('color'),
        geometryType: feature.getGeometry().getType(),
        // "Vazgeç" geometriyi eski haline döndürebilsin diye kopya alıyoruz.
        originalGeometry: feature.getGeometry().clone(),
      })
    }

    // Üzerine gelince imleç değişsin: tıklanabilir olduğu belli olsun.
    // İmleci kendi div'imize yazıyoruz; map.getTargetElement() harita
    // kapatıldıktan sonra undefined dönüyor ve temizlikte patlıyor.
    const element = containerRef.current

    function onPointerMove(event) {
      if (event.dragging) return
      const overFeature = map.hasFeatureAtPixel(event.pixel, {
        layerFilter: (layer) =>
          layer === featureLayerRef.current ||
          layer === poiLayerRef.current ||
          layer === stopLayerRef.current ||
          layer === vehicleLayerRef.current,
      })
      element.style.cursor = overFeature ? 'pointer' : ''
    }

    map.on('click', onClick)
    map.on('pointermove', onPointerMove)

    return () => {
      map.un('click', onClick)
      map.un('pointermove', onPointerMove)
      element.style.cursor = ''
    }
  }, [
    activeTool,
    pending,
    pendingPoi,
    selected,
    selectedPoi,
    selectedStop,
    mapRef,
    featureLayerRef,
    poiLayerRef,
    stopLayerRef,
    vehicleLayerRef,
  ])

  // --- Seçili objenin geometrisini düzenlenebilir yap ---
  useEffect(() => {
    const map = mapRef.current
    if (!map || !selected) return

    // Güncelleme yetkisi yoksa geometri de sürüklenemesin: kullanıcı
    // köşeleri oynatıp kaydedemeyince kafası karışırdı.
    if (!canUpdate) return

    // Collection'a sadece seçili feature'ı koyuyoruz: kullanıcı yanlışlıkla
    // komşu bir çizimin köşesini oynatamasın.
    const modify = new Modify({ features: new Collection([selected.feature]) })
    map.addInteraction(modify)

    return () => map.removeInteraction(modify)
  }, [selected, canUpdate, mapRef])

  // --- Çizim modunda imleci artı yap ---
  useEffect(() => {
    const element = containerRef.current
    if (!element || !activeTool) return

    element.style.cursor = 'crosshair'

    return () => {
      element.style.cursor = ''
    }
  }, [activeTool])

  // --- Isı haritasını aç/kapa ---
  useEffect(() => {
    // Katman haritada duruyor, sadece görünürlüğünü değiştiriyoruz:
    // her açılışta yeniden kurmak gereksiz istek demek olurdu.
    heatmapRef.current?.setVisible(isHeatmapOn)
  }, [isHeatmapOn, heatmapRef])

  // --- Seçili kaydı görünür kıl ---
  //
  // Kayıtlı çizimleri WMS gösteriyor, vektör katmanı görünmez. Ama
  // seçilen kaydı sürüklerken kullanıcının şekli görmesi gerekiyor.
  // Temizlik fonksiyonu bayrağı kaldırdığı için seçim nasıl biterse
  // bitsin (kaydet, vazgeç, Esc, sil) kayıt yeniden görünmez oluyor.
  // Yeni çizimin bayrağı ise drawend'de konuyor, aşağıda.
  useEffect(() => {
    const feature = selected?.feature
    if (!feature) return

    feature.set('interactive', true)
    return () => feature.unset('interactive')
  }, [selected])

  // --- Klavye kısayolları ---
  useEffect(() => {
    function onKeyDown(event) {
      // Ctrl/Cmd+Z: devam eden çizimin son kırılma noktasını siler.
      // Öncesinde tek çare Esc ile çizimi baştan başlatmaktı.
      if ((event.ctrlKey || event.metaKey) && event.key?.toLowerCase() === 'z') {
        // Çizim yoksa karışmıyoruz: kullanıcı bir metin kutusundaysa
        // tarayıcının kendi geri alması çalışmaya devam etsin.
        if (!drawRef.current) return

        // Draw henüz başlamadıysa removeLastPoint zaten sessizce dönüyor.
        event.preventDefault()
        drawRef.current.removeLastPoint()
        return
      }

      // Esc: devam eden çizimi ya da açık pencereyi iptal et.
      if (event.key !== 'Escape') return

      if (drawRef.current) {
        drawRef.current.abortDrawing()
      }
      setActiveTool(null)
      setIsConfirmingDelete(false)
      setIsConfirmingPoiDelete(false)
      setSelectedPoi(null)
      setSelectedStop(null)
      // Geometri değişikliği varsa geri alınsın diye state'i fonksiyonel
      // güncelliyoruz; effect'in bağımlılığına selected eklemeye gerek kalmıyor.
      setSelected((current) => {
        current?.feature.setGeometry(current.originalGeometry)
        return null
      })
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [])

  const handleCancel = useCallback(() => {
    if (pending) {
      // Kaydedilmeyen çizim haritada kalmasın.
      sourceRef.current.removeFeature(pending.feature)
    }
    setPending(null)
  }, [pending, sourceRef])

  const handleSave = useCallback(
    async (name, color) => {
      const { feature, geometryType } = pending
      const endpoint = ENDPOINT_BY_GEOMETRY[geometryType]

      // Harita 3857 -> veritabanı 4326 dönüşümü burada oluyor.
      const wkt = geometryToWkt(feature.getGeometry())
      const saved = await createFeature(endpoint, { name, wkt, color })

      feature.set('name', saved.name)
      feature.set('color', saved.color)
      feature.setId(`${endpoint}s-${saved.id}`)

      setCounts((prev) => ({
        ...prev,
        points: prev.points + (geometryType === 'Point' ? 1 : 0),
        lines: prev.lines + (geometryType === 'LineString' ? 1 : 0),
        polygons: prev.polygons + (geometryType === 'Polygon' ? 1 : 0),
      }))
      setPending(null)

      // Poligon kaydedildiyse içinde kalan envanteri hemen say.
      // await etmiyoruz: pencere kapansın, sonuç panelde belirsin.
      // Kendisini saymaması için id'sini hariç tutuyoruz.
      if (geometryType === 'Polygon') {
        runAnalysis(wkt, 'Kaydedilen poligon analizi', saved.id)
      }

      // WMS bir resim; kendiliğinden güncellenmiyor. Yeni çizimin genel
      // gösterime girmesi için katmanı sunucudan yeniden istiyoruz.
      //
      // Görünür işaretini hemen kaldırmıyoruz: yeni resim gelene kadar
      // (yarım saniye kadar) çizim ekrandan kaybolur, kullanıcı da
      // kaydının silindiğini sanırdı.
      refreshWmsLayer(featureWmsRef.current, () => feature.unset('interactive'))
      refreshWmsLayer(heatmapRef.current)
    },
    [pending, runAnalysis, featureWmsRef, heatmapRef],
  )

  /** POI formundan gelen bilgilerle kaydeder ve haritadaki işareti tamamlar. */
  const handleSavePoi = useCallback(
    async ({ name, categoryId, workingHours }) => {
      const { feature } = pendingPoi
      const wkt = geometryToWkt(feature.getGeometry())

      const saved = await createPoi({ name, wkt, categoryId, workingHours })

      feature.setId(`poi-${saved.id}`)
      feature.set('name', saved.name)
      feature.set('poi', saved)
      setPois((current) => [...current, saved])
      setPendingPoi(null)

      // POI gösterimi de sunucuda çizilen bir resim; yeni kaydın görünmesi
      // için tazelemek gerekiyor. Resim gelene kadar işaret görünür kalsın.
      refreshWmsLayer(poiWmsRef.current, () => feature.unset('interactive'))
      // Yoğunluk haritası noktaları sayıyor; POI eklenince tazelensin.
      refreshWmsLayer(heatmapRef.current)
    },
    [pendingPoi, poiWmsRef, heatmapRef],
  )

  // --- Ulaşım işlemleri ---

  /** Durak formundan gelen bilgilerle kaydeder ve haritayı tazeler. */
  const handleSaveStop = useCallback(
    async ({ name, routeId }) => {
      await createStop({ name, wkt: pendingStop.wkt, routeId })
      setPendingStop(null)
      setTransportError('')
      await loadRoutes()
    },
    [pendingStop, loadRoutes],
  )

  const handleSaveRoute = useCallback(
    async (form) => {
      const payload = { name: form.name.trim(), color: form.color, isActive: form.isActive }

      try {
        const saved = form.id === null
          ? await createRoute(payload)
          : await updateRoute(form.id, payload)

        setTransportError('')
        await loadRoutes()
        // Yeni eklenen güzergah hemen seçili gelsin: kullanıcının bir
        // sonraki işi zaten ona durak eklemek.
        setSelectedRouteId(saved.id)
      } catch (err) {
        setTransportError(err.response?.data?.message ?? 'Güzergah kaydedilemedi.')
      }
    },
    [loadRoutes],
  )

  const handleDeleteRoute = useCallback(
    async (route) => {
      if (!window.confirm(`"${route.name}" güzergahı silinsin mi?`)) return

      try {
        await deleteRoute(route.id)
        setTransportError('')
        setSelectedRouteId(null)
        await loadRoutes()
      } catch (err) {
        setTransportError(err.response?.data?.message ?? 'Güzergah silinemedi.')
      }
    },
    [loadRoutes],
  )

  const handleDeleteStop = useCallback(
    async (stop) => {
      if (!window.confirm(`"${stop.name}" durağı silinsin mi?`)) return

      try {
        await deleteStop(stop.id)
        setTransportError('')
        setSelectedStop(null)
        await loadRoutes()
      } catch (err) {
        setTransportError(err.response?.data?.message ?? 'Durak silinemedi.')
      }
    },
    [loadRoutes],
  )

  /**
   * Sürükle-bırak sonucu. Sunucuya tüm listeyi gönderip dönen sonuçla
   * çiziyoruz; iyimser güncelleme yapıp sunucu reddederse ekranla
   * veritabanı ayrışırdı.
   */
  const handleReorder = useCallback(
    async (routeId, stopIds) => {
      try {
        // Sunucu sırayı kaydedip rotayı OSRM'e yeniden hesaplatıyor.
        // Rota güncellenemediyse sıralama yine de kaydedilmiş oluyor ve
        // sebebi routeWarning ile geliyor.
        const updated = await reorderStops(routeId, stopIds)
        setTransportError(updated?.routeWarning ?? '')
        await loadRoutes()
      } catch (err) {
        setTransportError(err.response?.data?.message ?? 'Sıralama kaydedilemedi.')
      }
    },
    [loadRoutes],
  )

  /** "Rota Oluştur": durakların üzerinden geçen yolu OSRM'e hesaplatır. */
  const handleBuildRoute = useCallback(
    async (routeId) => {
      setIsBuildingRoute(true)

      try {
        await buildRoute(routeId)
        setTransportError('')
        await loadRoutes()
      } catch (err) {
        setTransportError(err.response?.data?.message ?? 'Rota oluşturulamadı.')
      } finally {
        setIsBuildingRoute(false)
      }
    },
    [loadRoutes],
  )

  /**
   * Bir güzergahın canlı yayınına katılır. Aynı anda tek hat takip
   * ediliyor: yenisine geçerken öncekinin grubundan çıkıyoruz, yoksa
   * artık izlenmeyen hattın konumları da gelmeye devam ederdi.
   */
  const trackRoute = useCallback(
    async (routeId) => {
      const connection = connectionRef.current
      if (!connection) return

      try {
        if (trackedRouteId !== null && trackedRouteId !== routeId) {
          await connection.invoke('LeaveRoute', trackedRouteId)
        }

        await connection.invoke('JoinRoute', routeId)
        setTrackedRouteId(routeId)
        setTransportError('')
      } catch {
        setTransportError('Canlı takip başlatılamadı.')
      }
    },
    [trackedRouteId],
  )

  const untrackRoute = useCallback(async () => {
    const connection = connectionRef.current

    if (connection && trackedRouteId !== null) {
      try {
        await connection.invoke('LeaveRoute', trackedRouteId)
      } catch {
        // Bağlantı zaten kopmuşsa grubu bırakmaya gerek de yok.
      }
    }

    setTrackedRouteId(null)
    setVehicleRouteId(null)
  }, [trackedRouteId])

  /** "Simülasyonu Başlat" - başlatan kişi aracı görsün diye takibe de alıyoruz. */
  const handleStartSimulation = useCallback(
    async (routeId) => {
      setIsSimulationBusy(true)

      try {
        const state = await startSimulation(routeId)
        setSimulations((current) => ({ ...current, [routeId]: state }))
        setTransportError('')
        await trackRoute(routeId)
      } catch (err) {
        setTransportError(err.response?.data?.message ?? 'Simülasyon başlatılamadı.')
      } finally {
        setIsSimulationBusy(false)
      }
    },
    [trackRoute],
  )

  const handleStopSimulation = useCallback(async (routeId) => {
    setIsSimulationBusy(true)

    try {
      await stopSimulation(routeId)
      setSimulations((current) => {
        const next = { ...current }
        delete next[routeId]
        return next
      })
      setVehicleRouteId(null)
      setTransportError('')
    } catch (err) {
      setTransportError(err.response?.data?.message ?? 'Simülasyon durdurulamadı.')
    } finally {
      setIsSimulationBusy(false)
    }
  }, [])

  /** Katman kontrolü: güzergahı haritada gösterir / gizler. */
  const handleToggleRouteVisible = useCallback((routeId) => {
    setHiddenRouteIds((current) => {
      const next = new Set(current)

      if (next.has(routeId)) {
        next.delete(routeId)
      } else {
        next.add(routeId)
      }

      return next
    })
  }, [])

  /** Listeden bir durağa tıklanınca haritada ona uçar ve bilgisini açar. */
  const handleFocusStop = useCallback(
    (stop) => {
      const geometry = wktToFeature(stop.wkt).getGeometry()

      mapRef.current.getView().animate({
        center: geometry.getCoordinates(),
        zoom: 15,
        duration: 700,
      })

      setSelectedStop(stop)
    },
    [mapRef],
  )

  /**
   * Analizi başlatır: kriterleri ve hedef bölgeyi WMS katmanının
   * parametrelerine yazıp katmanı görünür yapıyor. Hesabı GeoServer
   * yapıyor, biz yalnızca isteği kuruyoruz.
   */
  const runLocationAnalysis = useCallback(() => {
    const problem = validateAnalysis({ criteria, provinceId, areaWkt })

    if (problem) {
      setLocationError(problem)
      return
    }

    setLocationError('')

    const layer = locationAnalysisRef.current
    const source = layer.getSource()

    // updateParams hem parametreleri değiştiriyor hem yeniden çizdiriyor.
    // Kullanılmayan alan parametresini undefined yapıyoruz ki önceki
    // analizden kalan il/poligon isteğe eklenmesin.
    source.updateParams({
      criteria: criteriaToParam(criteria),
      provinceId: provinceId ?? undefined,
      areaWkt: provinceId ? undefined : areaWkt,
    })

    layer.setVisible(true)
    setIsAnalysisOn(true)

    // POI gösterimini soluklaştır: ısı haritası zaten POI'lerin olduğu
    // yerde yoğunlaşıyor, ikisi tam güçte üst üste binince desen okunmuyor.
    poiWmsRef.current?.setOpacity(0.35)

    // Haritayı hedef bölgeye oturt: Türkiye geneli görüntüde tek bir ilin
    // ısı haritası birkaç piksel kalıyor, kullanıcı sonucu göremiyor.
    const extent = locationAreaSourceRef.current?.getExtent()

    if (extent && Number.isFinite(extent[0])) {
      mapRef.current.getView().fit(extent, {
        padding: [40, 40, 40, 40],
        duration: 700,
        maxZoom: 12,
      })
    }
  }, [criteria, provinceId, areaWkt, locationAnalysisRef, locationAreaSourceRef, poiWmsRef, mapRef])

  /** Analizi ve hedef bölge seçimini sıfırlar. */
  const clearLocationAnalysis = useCallback(() => {
    locationAnalysisRef.current?.setVisible(false)
    poiWmsRef.current?.setOpacity(1)
    locationAreaSourceRef.current?.clear()
    setIsAnalysisOn(false)
    setProvinceId(null)
    setAreaWkt(null)
    setLocationError('')
    setActiveTool((tool) => (tool === 'LocationArea' ? null : tool))
  }, [locationAnalysisRef, locationAreaSourceRef, poiWmsRef])

  /**
   * Arama sonucuna tıklanınca o POI'ye uçar ve bilgi panelini açar.
   * Zoom 14: ilçe ölçeği - hem POI'nin çevresi görünüyor hem de SLD'deki
   * isim etiketi bu yakınlıkta devreye giriyor.
   */
  const handleSearchSelect = useCallback(
    (poi) => {
      const geometry = wktToFeature(poi.wkt).getGeometry()

      mapRef.current.getView().animate({
        center: geometry.getCoordinates(),
        zoom: 14,
        duration: 700,
      })

      setSelectedPoi(poi)
    },
    [mapRef],
  )

  /** POI'yi soft delete eder ve haritadaki işaretini kaldırır. */
  const handleDeletePoi = useCallback(async () => {
    await deletePoi(selectedPoi.id)

    // Kaydın haritadaki işaretini kaynaktan çıkarıyoruz; POI katmanı
    // vektör olduğu için WMS yenilemesi gerekmiyor.
    const source = poiSourceRef.current
    const feature = source.getFeatureById(`poi-${selectedPoi.id}`)
    if (feature) source.removeFeature(feature)

    setPois((current) => current.filter((p) => p.id !== selectedPoi.id))
    setIsConfirmingPoiDelete(false)
    setSelectedPoi(null)

    refreshWmsLayer(poiWmsRef.current)
    // Isı haritası noktaları sayıyor; POI silinince tazelensin.
    refreshWmsLayer(heatmapRef.current)
  }, [selectedPoi, poiSourceRef, poiWmsRef, heatmapRef])

  /** Vazgeçilirse haritaya konan geçici işaret kalkar. */
  const handleCancelPoi = useCallback(() => {
    if (pendingPoi) {
      poiSourceRef.current.removeFeature(pendingPoi.feature)
    }
    setPendingPoi(null)
  }, [pendingPoi, poiSourceRef])

  /**
   * Girilen enlem/boylama uçar ve orayı işaretler.
   * Girdi EPSG:4326; fromLonLat haritanın EPSG:3857 sistemine çeviriyor.
   */
  const handleSearch = useCallback((longitude, latitude) => {
    const center = fromLonLat([longitude, latitude])

    targetSourceRef.current.clear()
    targetSourceRef.current.addFeature(new Feature(new Point(center)))

    // Anında ışınlanmak yerine animasyon: kullanıcı haritanın nereden
    // nereye gittiğini takip edebilsin.
    mapRef.current.getView().animate({ center, zoom: 12, duration: 800 })
  }, [mapRef, targetSourceRef])

  /** Detay penceresindeki isim/renk ile haritada düzenlenen geometriyi birlikte kaydeder. */
  const handleUpdate = useCallback(
    async (name, color) => {
      const { feature, endpoint, id } = selected

      // Harita 3857 -> veritabanı 4326 dönüşümü burada oluyor.
      const wkt = geometryToWkt(feature.getGeometry())
      const saved = await updateFeature(endpoint, id, { name, wkt, color })

      feature.set('name', saved.name)
      feature.set('color', saved.color)
      setSelected(null)

      // İsim, renk veya geometri değişmiş olabilir; resmi tazeliyoruz.
      refreshWmsLayer(featureWmsRef.current)
      refreshWmsLayer(heatmapRef.current)
    },
    [selected, featureWmsRef, heatmapRef],
  )

  /** Vazgeç: harita üzerinde sürüklenen köşeleri eski haline döndürür. */
  const handleCancelEdit = useCallback(() => {
    if (selected) {
      selected.feature.setGeometry(selected.originalGeometry)
    }
    setSelected(null)
    setIsConfirmingDelete(false)
  }, [selected])

  /** Onaydan sonra soft delete: sunucu satırı silmiyor, is_deleted = true yapıyor. */
  const handleDelete = useCallback(async () => {
    const { feature, endpoint, id, countKey } = selected

    await deleteFeature(endpoint, id)

    sourceRef.current.removeFeature(feature)
    setCounts((prev) => ({ ...prev, [countKey]: Math.max(0, prev[countKey] - 1) }))
    setIsConfirmingDelete(false)
    setSelected(null)

    refreshWmsLayer(featureWmsRef.current)
    refreshWmsLayer(heatmapRef.current)
  }, [selected, sourceRef, featureWmsRef, heatmapRef])

  return (
    <div className="map-view">
      <div className="map-view__canvas" ref={containerRef} />

      <PoiSearch pois={pois} onSelect={handleSearchSelect} />

      <CoordinateSearch onSearch={handleSearch} />

      <DrawToolbar
        activeTool={activeTool}
        onSelect={setActiveTool}
        counts={counts}
        disabled={pending !== null || isLoading}
        canDelete={hasPermission('feature.delete')}
        isHeatmapOn={isHeatmapOn}
        onToggleHeatmap={() => setIsHeatmapOn((on) => !on)}
        isLocationOpen={isLocationOpen}
        onToggleLocation={() => setIsLocationOpen((open) => !open)}
        isRoutePanelOpen={isRoutePanelOpen}
        onToggleRoutePanel={() => setIsRoutePanelOpen((open) => !open)}
      />

      {isRoutePanelOpen && (
        <RoutePanel
          routes={routes}
          selectedRouteId={selectedRouteId}
          canManage={hasPermission('route.manage') || hasPermission('stop.manage')}
          isAddingStop={activeTool === 'Stop'}
          error={transportError}
          onSelectRoute={setSelectedRouteId}
          onSaveRoute={handleSaveRoute}
          onDeleteRoute={handleDeleteRoute}
          onToggleAddStop={() => setActiveTool((tool) => (tool === 'Stop' ? null : 'Stop'))}
          onReorder={handleReorder}
          onBuildRoute={handleBuildRoute}
          isBuildingRoute={isBuildingRoute}
          simulations={simulations}
          simulationNotice={simulationNotice}
          trackedRouteId={trackedRouteId}
          isSimulationBusy={isSimulationBusy}
          onStartSimulation={handleStartSimulation}
          onStopSimulation={handleStopSimulation}
          onTrack={trackRoute}
          onUntrack={untrackRoute}
          hiddenRouteIds={hiddenRouteIds}
          onToggleRouteVisible={handleToggleRouteVisible}
          onDeleteStop={handleDeleteStop}
          onFocusStop={handleFocusStop}
          onClose={() => setIsRoutePanelOpen(false)}
        />
      )}

      {/* Araç bilgi kutusu. Durum id ile tutuluyor, içerik canlı listeden
          okunuyor: yüzde her yayında kendiliğinden güncelleniyor. */}
      {vehicleRouteId !== null && simulations[vehicleRouteId] && (
        <VehicleInfoPanel
          vehicle={simulations[vehicleRouteId]}
          onClose={() => setVehicleRouteId(null)}
        />
      )}

      {pendingStop && (
        <SaveStopDialog
          routes={routes}
          defaultRouteId={selectedRouteId}
          onSave={handleSaveStop}
          onCancel={() => setPendingStop(null)}
        />
      )}

      {selectedStop && (
        <StopInfoPanel
          stop={selectedStop}
          canManage={hasPermission('stop.manage')}
          onDelete={() => handleDeleteStop(selectedStop)}
          onClose={() => setSelectedStop(null)}
        />
      )}

      {isLocationOpen && (
        <LocationAnalysisPanel
          categories={categories}
          provinces={provinces}
          provinceId={provinceId}
          areaWkt={areaWkt}
          isDrawing={activeTool === 'LocationArea'}
          criteria={criteria}
          isRunning={false}
          error={locationError}
          onProvinceChange={(id) => {
            setProvinceId(id)
            setLocationError('')
            if (id === null) locationAreaSourceRef.current?.clear()
          }}
          onDrawToggle={() =>
            setActiveTool((tool) => (tool === 'LocationArea' ? null : 'LocationArea'))
          }
          onCriteriaChange={setCriteria}
          onRun={runLocationAnalysis}
          onClear={clearLocationAnalysis}
          onClose={() => setIsLocationOpen(false)}
        />
      )}

      {/* Lejant yalnızca ısı haritası açıkken: kapalıyken açıklayacağı
          bir renk yok, boş yer kaplardı. */}
      {(isHeatmapOn || isAnalysisOn) && <HeatmapLegend />}

      {isLoading && (
        <p className="map-view__loading" role="status">
          Kayıtlı çizimler yükleniyor…
        </p>
      )}

      {error && (
        <p className="map-view__error" role="alert">
          {error}
        </p>
      )}

      {pendingPoi && (
        <SavePoiDialog
          categories={categories}
          onSave={handleSavePoi}
          onCancel={handleCancelPoi}
        />
      )}

      {/* POI bilgi paneli, çizim detay paneliyle aynı köşede duruyor;
          ikisi aynı anda açılamıyor (tıklama biri açıkken devre dışı). */}
      {selectedPoi && (
        <PoiInfoPanel
          poi={selectedPoi}
          canDelete={hasPermission('poi.manage')}
          onDelete={() => setIsConfirmingPoiDelete(true)}
          onClose={() => setSelectedPoi(null)}
        />
      )}

      {selectedPoi && isConfirmingPoiDelete && (
        <DeletePoiDialog
          name={selectedPoi.name}
          onDelete={handleDeletePoi}
          onCancel={() => setIsConfirmingPoiDelete(false)}
        />
      )}

      {pending && (
        <SaveFeatureDialog
          geometryType={pending.geometryType}
          onSave={handleSave}
          onCancel={handleCancel}
        />
      )}

      {/* Detay paneli açıkken analiz paneli gizleniyor: ikisi de sağ üstte
          duruyor ve üst üste binince alttakinin düğmelerine erişilemiyor.
          Sonuç state'te kalıyor, detay kapanınca panel geri geliyor. */}
      {analysis && !selected && !selectedPoi && !selectedStop && (
        <AnalysisPanel
          title={analysis.title}
          isLoading={analysis.isLoading}
          result={analysis.result}
          error={analysis.error}
          onClose={clearAnalysis}
        />
      )}

      {selected && (
        <FeatureDetailPanel
          feature={selected}
          onSave={handleUpdate}
          onCancel={handleCancelEdit}
          onDeleteRequest={() => setIsConfirmingDelete(true)}
        />
      )}

      {selected && isConfirmingDelete && (
        <DeleteFeatureDialog
          name={selected.name}
          geometryType={selected.geometryType}
          onDelete={handleDelete}
          onCancel={() => setIsConfirmingDelete(false)}
        />
      )}
    </div>
  )
}
