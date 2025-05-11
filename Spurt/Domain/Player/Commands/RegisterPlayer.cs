using Spurt.Data.Commands;

namespace Spurt.Domain.Player.Commands;

public interface IRegisterPlayer
{
    Task<Player> Execute(string name);
}

public class RegisterPlayer(IAddPlayer addPlayer) : IRegisterPlayer
{
    public async Task<Player> Execute(string name)
    {
        var player = new Player { Name = name };

        await addPlayer.Execute(player);

        return player;
    }
}