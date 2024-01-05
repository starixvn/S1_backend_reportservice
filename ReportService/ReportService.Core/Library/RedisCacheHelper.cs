using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ReportService.Core.Models.Enums;
using StackExchange.Redis;

namespace ReportService.Core.Library;

public class RedisCacheHelper : ICacheHelper
{
    private readonly ILogger<ICacheHelper> _logger;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly string[] _charsToRemove = { "[", "]", "/", " ", ",", "\r\n" };

    public RedisCacheHelper(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<ICacheHelper>>();
        _multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    }

    public TItem TryGetOrCreate<TItem>(string key, TimeSpan? timeSpan, Func<TItem> factory, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);

            if (!db.KeyExists(key))
            {
                var result = factory();
                if (HasData(result))
                {
                    db.StringSet(key, JsonConvert.SerializeObject(result), timeSpan);
                }
                return result;
            }

            var json = db.StringGet(key);
            return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : factory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return factory();
        }
    }

    public async Task<TItem> TryGetOrCreateAsync<TItem>(string key, TimeSpan? timeSpan, Func<Task<TItem>> factory, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);

            if (!db.KeyExists(key))
            {
                var result = await factory();
                if (HasData(result))
                {
                    await db.StringSetAsync(key, JsonConvert.SerializeObject(result), timeSpan);
                }
                return result;
            }

            var json = db.StringGet(key);
            return json.HasValue ? JsonConvert.DeserializeObject<TItem>(json) : await factory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return await factory();
        }
    }

    public async Task<bool> StringSetAsync(string key, RedisValue value, TimeSpan? timeSpan = null, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            return await db.StringSetAsync(key, value, timeSpan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    public bool KeyExists(string key, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            return db.KeyExists(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    public async Task<bool> KeyExistsAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    public async Task<TItem> StringGetAsync<TItem>(string key, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            var json = await db.StringGetAsync(key);
            return JsonConvert.DeserializeObject<TItem>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return default;
        }
    }

    public async Task<string> StringGetAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            var json = await db.StringGetAsync(key);
            return json.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return default;
        }
    }

    public IEnumerable<TItem> GetCacheList<TItem>(IEnumerable<string> keys, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        try
        {
            var taskList = new List<Task<RedisValue>>();
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            var trans = db.CreateTransaction();
            foreach (var key in keys)
            {
                var rediskey = $"{redisDataBase}:{RemoveSpecialChars(key)}";
                var task = db.StringGetAsync(rediskey);
                taskList.Add(task);
            }

            trans.Execute();

            var reslstList = Enumerable.Empty<TItem>();
            foreach (var task in taskList)
            {
                if (task.Result.HasValue)
                {
                    var data = JsonConvert.DeserializeObject<TItem>(task.Result);
                    reslstList = reslstList.Concat(new[] { data });
                }
            }
            return reslstList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{keys} | {ex.Message}");
            return default;
        }
    }

    public bool SetCacheList<TItem>(Dictionary<string, TItem> dataList, TimeSpan? timeSpan, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        try
        {
            var taskList = new List<Task<bool>>();
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            var trans = db.CreateTransaction();
            foreach (var data in dataList)
            {
                var key = $"{redisDataBase}:{RemoveSpecialChars(data.Key)}";
                var task = db.StringSetAsync(key, JsonConvert.SerializeObject(data.Value), timeSpan);
                taskList.Add(task);
            }
            return trans.Execute();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{dataList.Keys} | {ex.Message}");
            return default;
        }
    }

    public bool InsertList<TItem>(string key, TimeSpan? timeSpan, IEnumerable<TItem> list, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        try
        {
            if (list == null)
            {
                return false;
            }

            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            var db = _multiplexer.GetDatabase((int)redisDataBase);

            foreach (var item in list)
            {
                db.ListRightPush(key, JsonConvert.SerializeObject(item), flags: CommandFlags.FireAndForget);
            }
            db.KeyExpire(key, timeSpan);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    public IEnumerable<TItem> GetListByRange<TItem>(string key, long startIndex, long endIndex, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        try
        {
            var result = Enumerable.Empty<TItem>();
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            if (!db.KeyExists(key))
            {
                return result;
            }

            var list = db.ListRange(key, startIndex, endIndex);
            foreach (var item in list)
            {
                if (item.HasValue)
                {
                    var data = JsonConvert.DeserializeObject<TItem>(item);
                    result = result.Concat(new[] { data });
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return default;
        }
    }

    public async Task<IEnumerable<TItem>> GetListByRangeAsync<TItem>(string key, long startIndex, long endIndex,
        RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        try
        {
            var result = Enumerable.Empty<TItem>();
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            if (!db.KeyExists(key))
            {
                return result;
            }

            var list = await db.ListRangeAsync(key, startIndex, endIndex);
            foreach (var item in list)
            {
                if (item.HasValue)
                {
                    var data = JsonConvert.DeserializeObject<TItem>(item);
                    result = result.Concat(new[] { data });
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return default;
        }
    }

    public long GetListLength(string key, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        try
        {
            key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            return db.ListLength(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return default;
        }
    }

    public bool KeyDelete(string key, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            return db.KeyDelete(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    public async Task<bool> KeyDeleteAsync(string key, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        key = $"{redisDataBase}:{RemoveSpecialChars(key)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            return await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    public long KeyDeleteByPattern(string keyPattern, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        keyPattern = $"{redisDataBase}:{RemoveSpecialChars(keyPattern)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            var server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());
            var keys = server.Keys((int)redisDataBase, keyPattern).ToArray();
            return db.KeyDelete(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{keyPattern} | {ex.Message}");
            return 0;
        }
    }

    public async Task<long> KeyDeleteByPatternAsync(string keyPattern, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        keyPattern = $"{redisDataBase}:{RemoveSpecialChars(keyPattern)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            var server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());
            var keys = server.Keys((int)redisDataBase, keyPattern).ToArray();
            return await db.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{keyPattern} | {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> TryCreateLockAsync(RedisKey lockKey, RedisValue lockValue, TimeSpan? expiry, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        var key = $"{redisDataBase}:{RemoveSpecialChars(lockKey)}";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            return await db.StringSetAsync(key, lockValue, expiry, When.NotExists);// 使用When.NotExists選項，確保操作的原子性
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UnLockAsync(RedisKey lockKey, RedisValue lockValue, RedisDataBase redisDataBase = RedisDataBase.Admin)
    {
        //當鎖的值與預期的值相匹配時才釋放鎖，使用Lua script確保操作的原子性
        var key = $"{redisDataBase}:{RemoveSpecialChars(lockKey)}";
        var script = @"if redis.call('GET', KEYS[1]) == ARGV[1] then 
                       return redis.call('DEL', KEYS[1]) 
                   else 
                       return 0 
                   end";
        try
        {
            var db = _multiplexer.GetDatabase((int)redisDataBase);
            var result = await db.ScriptEvaluateAsync(script, new RedisKey[] { key }, new RedisValue[] { lockValue });
            return result.ToString() == "1";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{key} | {ex.Message}");
            return false;
        }
    }

    private string RemoveSpecialChars(string key)
    {
        foreach (var c in _charsToRemove)
        {
            key = key.Replace(c, string.Empty);
        }

        return key;
    }

    private bool HasData<T>(T obj)
    {
        if (obj == null)
        {
            return false;
        }

        return obj switch
        {
            ICollection collection => collection.Count > 0,
            string str => !string.IsNullOrEmpty(str),
            _ => true
        };
    }
}