using Spurt.Data.Commands;
using Spurt.Data.Queries;

namespace Spurt.Domain.Games.Commands;

public class StartGame(
    IGetGame getGame,
    IUpdateGame updateGame,
    IGameHubNotificationService gameHubNotificationService) : IStartGame
{
    public async Task<Game> Execute(string gameCode, Guid userId)
    {
        var game = await getGame.Execute(gameCode);

        if (game == null)
            throw new InvalidOperationException($"Game with code {gameCode} not found");

        var creator = game.Players.FirstOrDefault(p => p.IsCreator);
        if (creator == null || creator.UserId != userId)
            throw new InvalidOperationException("Only the game creator can start the game");

        if (!game.AllPlayersSubmittedCategories())
            throw new InvalidOperationException("All players must submit their categories before starting the game");

        game.State = GameState.InProgress;
        game.CurrentChoosingPlayerId = creator.Id;

        await updateGame.Execute(game);
        await gameHubNotificationService.NotifyGameUpdated(gameCode);

        return game;
    }
}

public interface IStartGame
{
    Task<Game> Execute(string gameCode, Guid userId);
}