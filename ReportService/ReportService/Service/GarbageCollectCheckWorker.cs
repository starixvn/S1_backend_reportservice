using System.Runtime;
using ReportService.Extensions;

namespace ReportService.Service;

public class GarbageCollectCheckWorker : BackgroundService
{
    private Timer _timer;
    private readonly ILogger<GarbageCollectCheckWorker> _logger;      
    private readonly long threshold;
    private readonly TimeSpan timeSpan;

    public GarbageCollectCheckWorker(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<GarbageCollectCheckWorker>>();
        // 單位換算：1GB = 1024 * 1024 * 1024 bytes
        threshold = 2L * 1024 * 1024 * 1024;
        timeSpan = TimeSpan.FromMinutes(10);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(GarbageCollectCheck, null, TimeSpan.Zero, TimeSpan.Zero);
        return Task.CompletedTask;
    }

    private void GarbageCollectCheck(object state)
    {
        try
        {
            // 取得當前記憶體用量
            var currentMemory = GC.GetTotalMemory(false);
            _logger.LogInformation($"GarbageCollectCheck: Current Total Memory: {currentMemory / 1024 / 1024 } MB");
            
            // 如果當前記憶體使用量超過 2GB，執行垃圾回收
            if (currentMemory > threshold)
            {
                _logger.LogInformation("GarbageCollectCheck: Memory usage exceeded 2GB. Performing full garbage collection...");
            
                // 強制執行垃圾回收
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            
                // 取得GC後記憶體用量
                var memoryAfterGC = GC.GetTotalMemory(false);
                _logger.LogInformation($"GarbageCollectCheck: Total Memory after forcing full GC: {memoryAfterGC / 1024 / 1024} MB");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, MethodExtension.GetCurrentMethod());
        }
        finally
        {
            _timer.Change(timeSpan, TimeSpan.Zero);
        }
    }
}