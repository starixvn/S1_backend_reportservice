using ReportService.Core.Models.Enums;
using StackExchange.Redis;

namespace ReportService.Core.Library;

public interface ICacheHelper
{
    TItem TryGetOrCreate<TItem>(string key, TimeSpan? timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<bool> StringSetAsync(string key, RedisValue value, TimeSpan? timeSpan = null, RedisDataBase redisDataBase = RedisDataBase.Admin);
    bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<bool> KeyExistsAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<string> StringGetAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Admin);
    IEnumerable<TItem> GetCacheList<TItem>(IEnumerable<string> keys, RedisDataBase redisDataBase = RedisDataBase.Admin);
    bool SetCacheList<TItem>(Dictionary<string, TItem> dataList, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Admin);
    bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list, RedisDataBase redisDataBase = RedisDataBase.Admin);
    IEnumerable<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<IEnumerable<TItem>> GetListByRangeAsync<TItem>(string key, long startIndex, long endIndex, RedisDataBase redisDataBase = RedisDataBase.Admin);
    long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Admin);
    bool KeyDelete(string key, RedisDataBase redisDataBase = RedisDataBase.Admin);
    long KeyDeleteByPattern(string keyPattern, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<long> KeyDeleteByPatternAsync(string keyPattern, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<bool> TryCreateLockAsync(RedisKey lockKey, RedisValue lockValue, TimeSpan? expiry, RedisDataBase redisDataBase = RedisDataBase.Admin);
    Task<bool> UnLockAsync(RedisKey lockKey, RedisValue lockValue, RedisDataBase redisDataBase = RedisDataBase.Admin);
}