using MapProject.Business.Exceptions;

namespace MapProject.Business.Analysis;

/// <summary>Tek bir analiz kriteri: hangi kategori, kaç puan.</summary>
public sealed record LocationCriterion(int CategoryId, int Weight);

/// <summary>
/// Kriter metnini ("4:70,5:30") çözer ve ödevin kurallarını uygular.
///
/// Kurallar sunucuda: arayüz zaten "Analizi başlat" düğmesini kilitliyor
/// ama istek doğrudan da atılabilir. Doğrulama tek yerde dursun diye burada.
/// </summary>
public static class LocationCriteria
{
    public const int MinCount = 2;
    public const int MaxCount = 5;
    public const int RequiredTotal = 100;

    public static IReadOnlyList<LocationCriterion> Parse(string? text)
    {
        var parts = (text ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var criteria = new List<LocationCriterion>();

        foreach (var part in parts)
        {
            var pair = part.Split(':', StringSplitOptions.TrimEntries);

            if (pair.Length != 2 ||
                !int.TryParse(pair[0], out var categoryId) ||
                !int.TryParse(pair[1], out var weight))
            {
                throw new InvalidUserOperationException(
                    $"Kriter biçimi hatalı: '{part}'. Beklenen: kategoriId:puan.");
            }

            if (categoryId <= 0)
            {
                throw new InvalidUserOperationException("Geçersiz kategori seçildi.");
            }

            if (weight <= 0)
            {
                throw new InvalidUserOperationException("Her kriterin puanı sıfırdan büyük olmalı.");
            }

            criteria.Add(new LocationCriterion(categoryId, weight));
        }

        if (criteria.Count < MinCount || criteria.Count > MaxCount)
        {
            throw new InvalidUserOperationException(
                $"En az {MinCount}, en fazla {MaxCount} kriter seçilmeli. Seçilen: {criteria.Count}.");
        }

        // Aynı kategori iki kez seçilirse SQL View'daki CASE ilk eşleşmeyi
        // alır ve ikinci puan sessizce yok sayılırdı.
        if (criteria.Select(c => c.CategoryId).Distinct().Count() != criteria.Count)
        {
            throw new InvalidUserOperationException("Aynı kategori birden fazla kez seçilemez.");
        }

        var total = criteria.Sum(c => c.Weight);

        if (total != RequiredTotal)
        {
            throw new InvalidUserOperationException(
                $"Puanların toplamı {RequiredTotal} olmalı. Şu an: {total}.");
        }

        return criteria;
    }
}
