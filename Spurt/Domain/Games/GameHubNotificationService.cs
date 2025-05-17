using Microsoft.AspNetCore.SignalR;

namespace Spurt.Domain.Games;

public interface IGameHubNotificationService
{
    Task NotifyPlayerJoined(string gameCode);
    Task NotifyGameStarted(string gameCode);
    Task NotifyCategorySubmitted(string gameCode);
    Task NotifyClueSelected(string gameCode);
}

public class GameHubNotificationService(IHubContext<GameHub> hubContext) : IGameHubNotificationService
{
    public async Task NotifyPlayerJoined(string gameCode)
    {
        await hubContext.Clients.Group(gameCode).SendAsync(GameHub.Events.PlayerJoined);
    }

    public async Task NotifyGameStarted(string gameCode)
    {
        await hubContext.Clients.Group(gameCode).SendAsync(GameHub.Events.GameStarted);
    }

    public async Task NotifyCategorySubmitted(string gameCode)
    {
        await hubContext.Clients.Group(gameCode).SendAsync(GameHub.Events.CategorySubmitted);
    }

    public async Task NotifyClueSelected(string gameCode)
    {
        await hubContext.Clients.Group(gameCode).SendAsync(GameHub.Events.ClueSelected);
    }
}