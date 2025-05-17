using Microsoft.AspNetCore.SignalR;

namespace Spurt.Domain.Games;

public interface IGameHubNotificationService
{
    Task NotifyGameUpdated(Game game);
}

public class GameHubNotificationService(IHubContext<GameHub> hubContext) : IGameHubNotificationService
{
    public async Task NotifyGameUpdated(Game game)
    {
        await hubContext.Clients.Group(game.Code).SendAsync(GameHub.Events.GameUpdated, game);
    }
}