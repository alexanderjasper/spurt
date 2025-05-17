using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Tests.Domain.Games.Commands;

public class PressBuzzerTests
{
    [Fact]
    public async Task Execute_WithValidBuzzerPress_RegistersPlayerAsBuzzed()
    {
        // Arrange
        var (gameCode, _, player, pressBuzzer, updateGame, notificationService) = CreateStandardTestSetup();

        // Act
        var result = await pressBuzzer.Execute(gameCode, player.Id);

        // Assert
        Assert.Equal(player.Id, result.BuzzedPlayerId);
        Assert.Equal(GameState.BuzzerPressed, result.State);
        Assert.NotNull(result.BuzzedTime);
        await updateGame.Received(1).Execute(Arg.Any<Game>());
        await notificationService.Received(1).NotifyGameUpdated(Arg.Any<Game>());
    }

    [Fact]
    public async Task Execute_WhenPlayerBuzzesForOwnCategory_ThrowsException()
    {
        // Arrange
        var (gameCode, game, _, pressBuzzer, _, _) = CreateStandardTestSetup();
        var categoryOwner = game.Players.First(p => p.Category != null);

        // Act/Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pressBuzzer.Execute(gameCode, categoryOwner.Id));
    }

    [Fact]
    public async Task Execute_WhenGameStateIsNotClueSelected_ThrowsException()
    {
        // Arrange
        var (gameCode, game, player, pressBuzzer, _, _) = CreateStandardTestSetup();

        // Change game state
        game.State = GameState.InProgress;

        // Act/Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pressBuzzer.Execute(gameCode, player.Id));
    }

    [Fact]
    public async Task Execute_WhenPlayerBuzzesAfterAnotherPlayer_DoesNotUpdateGame()
    {
        // Arrange
        var (gameCode, game, player, pressBuzzer, updateGame, _) = CreateStandardTestSetup();
        var otherPlayer = game.Players.First(p => p.Id != player.Id && p.Category == null);
        game.BuzzedPlayerId = otherPlayer.Id;
        game.BuzzedPlayer = otherPlayer;
        game.BuzzedTime = DateTime.UtcNow.AddSeconds(-1);
        game.State = GameState.BuzzerPressed;

        // Act
        var result = await pressBuzzer.Execute(gameCode, player.Id);

        // Assert
        Assert.Equal(otherPlayer.Id, result.BuzzedPlayerId);
        await updateGame.DidNotReceive().Execute(Arg.Any<Game>());
    }

    private static (string gameCode, Game game, Player player, PressBuzzer pressBuzzer,
        IUpdateGame updateGame, IGameHubNotificationService notificationService)
        CreateStandardTestSetup()
    {
        const string gameCode = "TEST123";

        // Create users
        var user1 = new User { Id = Guid.NewGuid(), Name = "Player 1" };
        var user2 = new User { Id = Guid.NewGuid(), Name = "Player 2" };
        var categoryOwnerUser = new User { Id = Guid.NewGuid(), Name = "Category Owner" };

        // Create game
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.ClueSelected,
            Players = [],
        };

        // Create players
        var player1 = new Player
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            User = user1,
            GameId = game.Id,
            Game = game,
        };

        var player2 = new Player
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            User = user2,
            GameId = game.Id,
            Game = game,
        };

        var categoryOwner = new Player
        {
            Id = Guid.NewGuid(),
            UserId = categoryOwnerUser.Id,
            User = categoryOwnerUser,
            GameId = game.Id,
            Game = game,
        };

        // Add players to game
        game.Players.Add(player1);
        game.Players.Add(player2);
        game.Players.Add(categoryOwner);

        // Create category and clue
        var category = new Category
        {
            Id = Guid.NewGuid(),
            PlayerId = categoryOwner.Id,
            Player = categoryOwner,
            Title = "Test Category",
            IsSubmitted = true,
        };

        categoryOwner.Category = category;

        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            Answer = "Test Answer",
            Question = "Test Question",
            PointValue = 200,
        };

        category.Clues = [clue];

        // Set selected clue in game
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;

        // Create mocks
        var getGame = Substitute.For<IGetGame>();
        getGame.Execute(gameCode).Returns(game);

        var updateGame = Substitute.For<IUpdateGame>();
        updateGame.Execute(Arg.Any<Game>()).Returns(args => args.Arg<Game>());

        var notificationService = Substitute.For<IGameHubNotificationService>();
        notificationService.NotifyGameUpdated(Arg.Any<Game>()).Returns(Task.CompletedTask);

        // Create command
        var pressBuzzer = new PressBuzzer(getGame, updateGame, notificationService);

        return (gameCode, game, player1, pressBuzzer, updateGame, notificationService);
    }
}