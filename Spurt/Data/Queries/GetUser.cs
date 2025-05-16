using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Users;

namespace Spurt.Data.Queries;

public class GetUser(AppDbContext dbContext) : IGetUser
{
    public async Task<User?> Execute(Guid userId)
    {
        return await dbContext.Users
            .Include(u => u.Players)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}

public interface IGetUser
{
    Task<User?> Execute(Guid userId);
}