import { createContext } from 'react'

// Context nesnesi ayrı dosyada: bir dosya hem bileşen hem başka şeyler
// export ederse Vite'ın Fast Refresh'i (kaydedince anlık güncelleme) bozuluyor.
export const AuthContext = createContext(null)
