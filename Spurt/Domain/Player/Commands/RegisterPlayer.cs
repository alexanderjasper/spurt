namespace Spurt.Domain.Player.Commands;

public interface IRegisterPlayer
{
    Player Execute(string name);
}

public class RegisterPlayer : IRegisterPlayer
{
    public Player Execute(string name)
    {
        var player = new Player { Name = name };
        return player;
    }
}