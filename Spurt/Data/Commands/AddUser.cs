using Spurt.Domain.Users;

namespace Spurt.Data.Commands;

public class AddUser(AppDbContext dbContext) : IAddUser
{
    public async Task Execute(User user)
    {
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }
}

public interface IAddUser
{
    Task Execute(User user);
}