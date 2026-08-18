/**
 * Metni karşılaştırmaya hazırlar.
 *
 * Türkçede büyük/küçük harf dönüşümünün bir tuzağı var: JavaScript'in
 * varsayılan toLowerCase'i 'I' harfini 'i' yapıyor, oysa Türkçede karşılığı
 * 'ı'. Aynı şekilde 'İ' de bozuluyor. toLocaleLowerCase('tr') doğru eşlemeyi
 * yaptığı için "ISPARTA" arayan da "Isparta"yı buluyor.
 */
export function normalize(text) {
  return (text ?? '').toLocaleLowerCase('tr').trim()
}

/**
 * Verilen alanlardan herhangi biri aramayı içeriyorsa true döner.
 * Arama boşsa herkes geçer - filtre yokmuş gibi davranır.
 *
 * @param {string} query kullanıcının yazdığı metin
 * @param {...(string|null|undefined)} fields taranacak alanlar
 */
export function matchesQuery(query, ...fields) {
  const needle = normalize(query)
  if (!needle) return true

  return fields.some((field) => normalize(field).includes(needle))
}
