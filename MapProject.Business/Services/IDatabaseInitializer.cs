namespace MapProject.Business.Services;

public interface IDatabaseInitializer
{
    /// <summary>Bekleyen migration'ları uygular ve tohum kullanıcıları oluşturur.</summary>
    Task InitializeAsync();
}
