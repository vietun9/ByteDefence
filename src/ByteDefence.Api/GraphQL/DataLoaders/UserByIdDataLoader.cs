using ByteDefence.Api.Data;
using ByteDefence.Shared.Models;
using GreenDonut;
using Microsoft.EntityFrameworkCore;

namespace ByteDefence.Api.GraphQL.DataLoaders;

/// <summary>
/// Batch data loader for users to solve N+1 query problem.
/// Groups multiple user lookups into a single database query.
/// </summary>
public class UserByIdDataLoader : BatchDataLoader<string, User?>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UserByIdDataLoader(
        IDbContextFactory<AppDbContext> contextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _contextFactory = contextFactory;
    }

    protected override async Task<IReadOnlyDictionary<string, User?>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var users = await context.Users
            .Where(u => keys.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        // Return dictionary with null for missing keys
        return keys.ToDictionary(
            key => key,
            key => users.TryGetValue(key, out var user) ? user : null);
    }
}
