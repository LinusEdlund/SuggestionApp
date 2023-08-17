﻿

// Ignore Spelling: Mongo

using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLibrary.DataAccess;
public class MongoStatusData : IStatusData
{
    private readonly IMemoryCache _cache;
    private const string CacheName = "statusData";
    private readonly IMongoCollection<StatusModel> _statuses;

    public MongoStatusData(IDbConnection db, IMemoryCache cache)
    {
        _cache = cache;
        _statuses = db.StatusCollection;
    }

    public async Task<List<StatusModel>> GetAllStatuses()
    {
        var output = _cache.Get<List<StatusModel>>(CacheName);

        if (output is null || output.Count <= 0)
        {
            var results = await _statuses.FindAsync(_ => true);
            output = results.ToList();

            _cache.Set(CacheName, output, TimeSpan.FromDays(1));
        }

        return output;
    }

    public Task CreateStatus(StatusModel status)
    {
        return _statuses.InsertOneAsync(status);
    }

}
