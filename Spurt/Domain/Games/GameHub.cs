using Microsoft.AspNetCore.SignalR;

namespace Spurt.Domain.Games;

public class GameHub : Hub
{
    public static class Methods
    {
        public const string JoinGameGroup = nameof(JoinGameGroup);
    }

    public static class Events
    {
        public const string PlayerJoined = nameof(PlayerJoined);
        public const string CategorySubmitted = nameof(CategorySubmitted);
        public const string GameStarted = nameof(GameStarted);
        public const string ClueSelected = nameof(ClueSelected);
    }

    public async Task JoinGameGroup(string gameCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameCode);
    }
}