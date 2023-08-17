

using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLibrary.DataAccess;
public class MongoSuggestionData : ISuggestionData
{
    private readonly IDbConnection _db;
    private readonly IUserData _userData;
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<SuggestionModel> _suggestions;
    private const string CacheName = "SuggestionData";

    public MongoSuggestionData(IDbConnection db,
                               IUserData userData,
                               IMemoryCache cache)
    {
        _db = db;
        _userData = userData;
        _cache = cache;
        _suggestions = db.SuggestionCollection;
    }

    public async Task<List<SuggestionModel>> GetAllSuggestions()
    {
        var output = _cache.Get<List<SuggestionModel>>(CacheName);

        if (output is null)
        {
            var results = await _suggestions.FindAsync(s => s.Archived == false);
            output = results.ToList();

            _cache.Set(CacheName, output, TimeSpan.FromMinutes(1));
        }

        return output;
    }

    public async Task<List<SuggestionModel>> GetUsersSuggestions(string userId)
    {
        var output = _cache.Get<List<SuggestionModel>>(userId);
        if (output is null)
        {
            var results = await _suggestions.FindAsync(s => s.Author.Id == userId);
            output = results.ToList();

            _cache.Set(userId, output, TimeSpan.FromMinutes(1));
        }

        return output;
    }

    public async Task<List<SuggestionModel>> GetAllApprovedSuggestions()
    {
        var output = await GetAllSuggestions();
        return output.Where(s => s.ApprovedForRelease).ToList();
    }

    public async Task<SuggestionModel> GetSuggestion(string id)
    {
        return (await _suggestions.FindAsync(s => s.Id == id)).FirstOrDefault();
    }

    public async Task<List<SuggestionModel>> GetAllSuggestionsWaitForApproval()
    {
        var output = await GetAllSuggestions();
        return output.Where(s => !s.ApprovedForRelease && !s.Rejected).ToList();
    }

    public async Task UpdateSuggestion(SuggestionModel suggestion)
    {
        await _suggestions.ReplaceOneAsync(s => s.Id == suggestion.Id, suggestion);
        _cache.Remove(CacheName);
    }

    public async Task UpVoteSuggestion(string suggestionId, string userId)
    {
        var client = _db.Client;

        using var session = await client.StartSessionAsync();

        session.StartTransaction();

        try
        {
            var db = client.GetDatabase(_db.DbName);
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            var suggestion = (await suggestionsInTransaction.FindAsync(s => s.Id == suggestionId)).First();

            bool isUpVote = suggestion.UserVotes.Add(userId);

            if (isUpVote == false)
            {
                suggestion.UserVotes.Remove(userId);
            }

            await suggestionsInTransaction.ReplaceOneAsync(session, s => s.Id == suggestionId, suggestion);


            var usersInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
            var user = await _userData.GetUserById(userId);

            if (isUpVote)
            {
                user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
            }
            else
            {
                var suggestionToRemove = user.VotedOnSuggestions.Where(s => s.Id == suggestionId).First();
                user.VotedOnSuggestions.Remove(suggestionToRemove);
            }


            await usersInTransaction.ReplaceOneAsync(session, u => u.Id == userId, user);

            await session.CommitTransactionAsync();

            _cache.Remove(CacheName);

        }
        catch (Exception ex)
        {
            //log here

            await session.AbortTransactionAsync();
            throw;
        }

    }

    public async Task CreateSuggestion(SuggestionModel suggestion)
    {
        var client = _db.Client;

        using var session = await client.StartSessionAsync();

        session.StartTransaction();

        try
        {
            var db = client.GetDatabase(_db.DbName);
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            await suggestionsInTransaction.InsertOneAsync(session, suggestion);


            var usersInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
            var user = await _userData.GetUserById(suggestion.Author.Id);
            user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));

            await usersInTransaction.ReplaceOneAsync(session, u => u.Id == user.Id, user);

            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            throw;
        }

    }

}

