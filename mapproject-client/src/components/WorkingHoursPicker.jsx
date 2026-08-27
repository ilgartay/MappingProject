import { useState } from 'react'
import { DAYS, TIMES, DEFAULT_HOURS, formatWorkingHours } from './workingHours'
import './WorkingHoursPicker.css'

/**
 * Mesai saati seçici: günler tik kutusu, saatler açılır kutu.
 *
 * Neden serbest metin değil: alan veritabanında serbest metin
 * (varchar 100) ama kullanıcı elle yazdığında "09.00- 23.00",
 * "her gun 9-6" gibi birbirini tutmayan değerler giriyordu -
 * demo verisinde bile böyle bir kayıt var. Seçtirmek, aramanın ve
 * ileride "şu an açık mı" gibi bir sorunun tek biçimli veriyle
 * çalışmasını sağlıyor.
 */

/**
 * Seçimleri kendi içinde tutar, dışarıya yalnızca biçimlenmiş metni
 * verir - kaydeden formun günlerle uğraşması gerekmiyor. Hiçbir gün
 * seçili değilse boş metin gider; form bunu "eksik" diye yakalıyor.
 */
export default function WorkingHoursPicker({ onChange }) {
  const [value, setValue] = useState(DEFAULT_HOURS)

  function update(changes) {
    const next = { ...value, ...changes }
    setValue(next)
    onChange(formatWorkingHours(next))
  }

  function toggleDay(key) {
    update({
      days: value.days.includes(key)
        ? value.days.filter((d) => d !== key)
        : [...value.days, key].sort((a, b) => a - b),
    })
  }

  const preview = formatWorkingHours(value)

  return (
    <div className="hours-picker">
      <label className="hours-picker__chip">
        <input
          type="checkbox"
          checked={value.allDay}
          onChange={(e) => update({ allDay: e.target.checked })}
        />
        <span>7/24 açık</span>
      </label>

      {/* 7/24 seçiliyse gün ve saat sorulmuyor: ikisi birbiriyle
          çelişen bilgi olurdu. */}
      <fieldset className="hours-picker__group" disabled={value.allDay}>
        <div className="hours-picker__days">
          {DAYS.map((day) => (
            <label key={day.key} className="hours-picker__chip">
              <input
                type="checkbox"
                checked={value.days.includes(day.key)}
                onChange={() => toggleDay(day.key)}
              />
              <span>{day.short}</span>
            </label>
          ))}
        </div>

        <div className="hours-picker__times">
          <select
            aria-label="Açılış saati"
            value={value.opensAt}
            onChange={(e) => update({ opensAt: e.target.value })}
          >
            {TIMES.map((time) => (
              <option key={time} value={time}>{time}</option>
            ))}
          </select>
          <span className="hours-picker__dash">–</span>
          <select
            aria-label="Kapanış saati"
            value={value.closesAt}
            onChange={(e) => update({ closesAt: e.target.value })}
          >
            {TIMES.map((time) => (
              <option key={time} value={time}>{time}</option>
            ))}
          </select>
        </div>
      </fieldset>

      {/* Kaydedilen değer bir metin; ne yazılacağını önceden göstermek
          kullanıcıyı "acaba nasıl kaydoldu" sorusundan kurtarıyor. */}
      <p className="hours-picker__preview">
        {preview || 'En az bir gün seçin.'}
      </p>
    </div>
  )
}
