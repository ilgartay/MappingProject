import { useCallback, useSyncExternalStore } from 'react'

/**
 * CSS medya sorgusunu JS tarafında dinler.
 * Dekoratif haritayı dar ekranda CSS ile gizlemek yerine hiç kurmuyoruz:
 * gizli bir OpenLayers haritası boşuna döşeme indirir.
 *
 * useSyncExternalStore, React dışındaki bir kaynağa (burada matchMedia)
 * abone olmanın doğru yolu: useState + useEffect ikilisinin aksine ilk
 * render'da da doğru değeri veriyor ve ekstra render tetiklemiyor.
 */
export function useMediaQuery(query) {
  const subscribe = useCallback(
    (onStoreChange) => {
      const mediaQuery = window.matchMedia(query)
      mediaQuery.addEventListener('change', onStoreChange)

      return () => mediaQuery.removeEventListener('change', onStoreChange)
    },
    [query],
  )

  const getSnapshot = useCallback(() => window.matchMedia(query).matches, [query])

  return useSyncExternalStore(subscribe, getSnapshot)
}
