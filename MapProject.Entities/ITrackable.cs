namespace MapProject.Entities;

/// <summary>
/// Değişiklik zamanı takip edilen kayıtlar.
/// AppDbContext.SaveChanges bu arayüzü uygulayan her değişen satıra
/// modified_date damgasını kendisi vuruyor.
/// </summary>
public interface IModifiable
{
    DateTime? ModifiedDate { get; set; }
}

/// <summary>
/// Çizim tablolarının ortak sözleşmesi: kimlik, öznitelikler ve izleme
/// kolonları. Üç geometri tablosu da bunu uyguluyor; böylece servis ve
/// DbContext tarafında tek bir kod yolu yetiyor (geometri tipi hariç).
/// </summary>
public interface ITrackable : IModifiable
{
    int Id { get; set; }
    string Name { get; set; }
    string Color { get; set; }

    /// <summary>Kaydı oluşturan kullanıcı.</summary>
    int InsertedUserId { get; set; }

    DateTime InsertedDate { get; set; }

    /// <summary>Soft delete: satır durur, listelerde görünmez.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Geçici olarak pasife alma; silmekten farkı geri açılabilmesi.</summary>
    bool IsActive { get; set; }
}
