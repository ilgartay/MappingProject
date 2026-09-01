using System.Collections.Concurrent;
using MapProject.Business.Exceptions;
using MapProject.Business.Settings;
using MapProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MapProject.Business.Simulation;

public class SimulationService : ISimulationService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISimulationBroadcaster _broadcaster;
    private readonly SimulationSettings _settings;
    private readonly ILogger<SimulationService> _logger;

    // Güzergah başına en fazla bir simülasyon. Aynı hatta iki araç
    // koşsaydı takipçiler hangisinin konumunu gördüklerini bilemezdi.
    private readonly ConcurrentDictionary<int, SimulationRun> _runs = new();

    public SimulationService(
        IServiceScopeFactory scopeFactory,
        ISimulationBroadcaster broadcaster,
        IOptions<SimulationSettings> settings,
        ILogger<SimulationService> logger)
    {
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<VehicleState> StartAsync(int routeId, CancellationToken cancellationToken = default)
    {
        // Tekil servis olduğumuz için DbContext'i doğrudan enjekte
        // edemiyoruz: DbContext scoped ve iş parçacığı güvenli değil.
        // Her okuma için kendi kapsamımızı açıyoruz.
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var route = await context.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == routeId, cancellationToken)
            ?? throw new InvalidUserOperationException("Seçilen güzergah bulunamadı.");

        if (route.RouteGeometry is null)
        {
            throw new InvalidUserOperationException(
                "Bu güzergahın rotası yok. Önce 'Rota Oluştur' ile rotayı üretin.");
        }

        Stop(routeId);

        var path = new RoutePath(route.RouteGeometry);

        // Süre gerçek sürüş süresinden türüyor; OSRM vermediyse hattın
        // uzunluğundan 50 km/sa varsayarak kabaca hesaplıyoruz.
        var realSeconds = route.RouteDuration ?? path.TotalMetres / (50_000.0 / 3600);
        var seconds = Math.Clamp(
            realSeconds / _settings.SpeedFactor, _settings.MinSeconds, _settings.MaxSeconds);

        var run = new SimulationRun
        {
            RouteId = route.Id,
            RouteName = route.Name,
            RouteColor = route.Color,
            Path = path,
            MetresPerSecond = path.TotalMetres / seconds,
            StartedAt = DateTime.UtcNow,
            Cancellation = new CancellationTokenSource()
        };

        _runs[routeId] = run;

        // Döngüyü beklemiyoruz: "başlat" isteği hemen dönmeli, araç
        // arka planda yürümeye devam etmeli.
        _ = Task.Run(() => RunAsync(run), run.Cancellation.Token);

        _logger.LogInformation(
            "Simülasyon başladı: {Route} ({Metres:F0} m, {Seconds:F0} sn)",
            route.Name, path.TotalMetres, seconds);

        return Snapshot(run, travelled: 0, finished: false);
    }

    public bool Stop(int routeId)
    {
        if (!_runs.TryRemove(routeId, out var run)) return false;

        run.Cancellation.Cancel();
        run.Cancellation.Dispose();
        return true;
    }

    public IReadOnlyList<VehicleState> GetActive() =>
        _runs.Values.Select(run => Snapshot(run, run.Travelled, finished: false)).ToList();

    /// <summary>Aracı düzenli aralıklarla ilerletip her adımda yayınlar.</summary>
    private async Task RunAsync(SimulationRun run)
    {
        var interval = TimeSpan.FromMilliseconds(_settings.TickMilliseconds);
        using var timer = new PeriodicTimer(interval);
        var token = run.Cancellation.Token;

        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                run.Travelled += run.MetresPerSecond * interval.TotalSeconds;

                var finished = run.Travelled >= run.Path.TotalMetres;
                if (finished) run.Travelled = run.Path.TotalMetres;

                await _broadcaster.PublishAsync(Snapshot(run, run.Travelled, finished), token);

                if (finished)
                {
                    _logger.LogInformation("Simülasyon bitti: {Route}", run.RouteName);
                    Stop(run.RouteId);
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Durdurma isteği; beklenen son.
        }
        catch (Exception ex)
        {
            // Yakalamazsak arka plan görevindeki hata sessizce yutulur ve
            // araç açıklamasız donar.
            _logger.LogError(ex, "Simülasyon hatası: {Route}", run.RouteName);
            Stop(run.RouteId);
        }
    }

    private static VehicleState Snapshot(SimulationRun run, double travelled, bool finished)
    {
        var position = run.Path.PointAt(travelled);

        // Yön için biraz ilerideki noktaya bakıyoruz: iki ardışık
        // koordinat çok yakın olabiliyor ve aradaki açı gürültülü çıkıyor.
        var ahead = run.Path.PointAt(Math.Min(travelled + 25, run.Path.TotalMetres));

        return new VehicleState
        {
            RouteId = run.RouteId,
            RouteName = run.RouteName,
            RouteColor = run.RouteColor,
            Longitude = position.X,
            Latitude = position.Y,
            Progress = run.Path.TotalMetres <= 0 ? 100 : travelled / run.Path.TotalMetres * 100,
            TravelledMetres = travelled,
            TotalMetres = run.Path.TotalMetres,
            Heading = Bearing(position.X, position.Y, ahead.X, ahead.Y),
            StartedAt = run.StartedAt,
            IsFinished = finished
        };
    }

    /// <summary>Kuzeyden saat yönünde derece (0 = kuzey, 90 = doğu).</summary>
    private static double Bearing(double lon1, double lat1, double lon2, double lat2)
    {
        // Boylam farkı enleme göre daralıyor; kosinüsle düzeltmezsek
        // ok kuzeye doğru sapıyor.
        var dx = (lon2 - lon1) * Math.Cos(lat1 * Math.PI / 180);
        var dy = lat2 - lat1;

        if (dx == 0 && dy == 0) return 0;

        var degrees = Math.Atan2(dx, dy) * 180 / Math.PI;
        return degrees < 0 ? degrees + 360 : degrees;
    }

    public void Dispose()
    {
        foreach (var routeId in _runs.Keys) Stop(routeId);
        GC.SuppressFinalize(this);
    }

    private sealed class SimulationRun
    {
        public required int RouteId { get; init; }
        public required string RouteName { get; init; }
        public required string RouteColor { get; init; }
        public required RoutePath Path { get; init; }
        public required double MetresPerSecond { get; init; }
        public required DateTime StartedAt { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }

        public double Travelled { get; set; }
    }
}
