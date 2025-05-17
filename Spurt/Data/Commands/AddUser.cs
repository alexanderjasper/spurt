using Spurt.Domain.Users;

namespace Spurt.Data.Commands;

public class AddUser(AppDbContext dbContext) : IAddUser
{
    public async Task<User> Execute(User user)
    {
        var result = await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }
}

public interface IAddUser
{
    Task<User> Execute(User user);
}