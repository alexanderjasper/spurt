using Spurt.Data.Commands;

namespace Spurt.Domain.Users.Commands;

public class RegisterUser(IAddUser addUser) : IRegisterUser
{
    public async Task<User> Execute(string name)
    {
        var user = new User { Name = name };

        await addUser.Execute(user);

        return user;
    }
}

public interface IRegisterUser
{
    Task<User> Execute(string name);
}