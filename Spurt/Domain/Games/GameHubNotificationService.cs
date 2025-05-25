using Microsoft.AspNetCore.SignalR;

namespace Spurt.Domain.Games;

public interface IGameHubNotificationService
{
    Task NotifyGameUpdated(string gameCode);
}

public class GameHubNotificationService(IHubContext<GameHub> hubContext) : IGameHubNotificationService
{
    public async Task NotifyGameUpdated(string gameCode)
    {
        await hubContext.Clients.Group(gameCode).SendAsync(GameHub.Events.GameUpdated);
    }
}