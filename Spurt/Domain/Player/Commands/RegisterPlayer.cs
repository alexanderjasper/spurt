using Spurt.Data.Commands;

namespace Spurt.Domain.Player.Commands;

public interface IRegisterPlayer
{
    Player Execute(string name);
}

public class RegisterPlayer(IAddPlayer addPlayer) : IRegisterPlayer
{
    public Player Execute(string name)
    {
        var player = new Player { Name = name };
        
        addPlayer.Execute(player);
        
        return player;
    }
}