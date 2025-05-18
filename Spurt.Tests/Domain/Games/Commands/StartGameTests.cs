using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Tests.Domain.Games.Commands;

public class StartGameTests
{
    private readonly IGetGame _getGame = Substitute.For<IGetGame>();
    private readonly IUpdateGame _updateGame = Substitute.For<IUpdateGame>();
    private readonly IGameHubNotificationService _notificationService = Substitute.For<IGameHubNotificationService>();
    private readonly StartGame _sut;

    public StartGameTests()
    {
        _sut = new StartGame(_getGame, _updateGame, _notificationService);
    }

    [Fact]
    public async Task Execute_WithOnePlayer_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var userId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
        };

        var creator = new Player
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = new User { Id = userId, Name = "Creator" },
            IsCreator = true,
            Game = game,
            GameId = game.Id,
        };

        var category = new Category
        {
            Title = "Category",
            PlayerId = creator.Id,
            Player = creator,
            IsSubmitted = true,
        };

        creator.Category = category;
        game.Players = [creator];
        _getGame.Execute(gameCode).Returns(game);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, userId));

        // Assert
        Assert.Equal("At least 2 players are required to start the game", exception.Message);
    }

    [Fact]
    public async Task Execute_WithTwoPlayers_StartsGame()
    {
        // Arrange
        const string gameCode = "ABCD";
        var creatorId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
        };

        var creator = new Player
        {
            Id = Guid.NewGuid(),
            UserId = creatorId,
            User = new User { Id = creatorId, Name = "Creator" },
            IsCreator = true,
            Game = game,
            GameId = game.Id,
        };

        var player = new Player
        {
            Id = Guid.NewGuid(),
            UserId = playerId,
            User = new User { Id = playerId, Name = "Player" },
            IsCreator = false,
            Game = game,
            GameId = game.Id,
        };

        var category1 = new Category
        {
            Title = "Category 1",
            PlayerId = creator.Id,
            Player = creator,
            IsSubmitted = true,
        };

        var category2 = new Category
        {
            Title = "Category 2",
            PlayerId = player.Id,
            Player = player,
            IsSubmitted = true,
        };

        creator.Category = category1;
        player.Category = category2;
        game.Players = [creator, player];

        _getGame.Execute(gameCode).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(game);

        // Act
        var result = await _sut.Execute(gameCode, creatorId);

        // Assert
        Assert.Equal(GameState.InProgress, result.State);
        Assert.Equal(creator.Id, result.CurrentChoosingPlayerId);

        await _updateGame.Received(1).Execute(Arg.Any<Game>());
        await _notificationService.Received(1).NotifyGameUpdated(Arg.Any<Game>());
    }

    [Fact]
    public async Task Execute_WhenGameDoesNotExist_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var userId = Guid.NewGuid();

        _getGame.Execute(gameCode).Returns(Task.FromResult<Game?>(null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, userId));

        Assert.Equal($"Game with code {gameCode} not found", exception.Message);
    }

    [Fact]
    public async Task Execute_WhenUserIsNotCreator_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var creatorId = Guid.NewGuid();
        var nonCreatorId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
        };

        var creator = new Player
        {
            Id = Guid.NewGuid(),
            UserId = creatorId,
            User = new User { Id = creatorId, Name = "Creator" },
            IsCreator = true,
            Game = game,
            GameId = game.Id,
        };

        game.Players = [creator];

        _getGame.Execute(gameCode).Returns(game);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, nonCreatorId));

        Assert.Equal("Only the game creator can start the game", exception.Message);
    }

    [Fact]
    public async Task Execute_WhenNotAllPlayersSubmittedCategories_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var creatorId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
        };

        var creator = new Player
        {
            Id = Guid.NewGuid(),
            UserId = creatorId,
            User = new User { Id = creatorId, Name = "Creator" },
            IsCreator = true,
            Game = game,
            GameId = game.Id,
        };

        var player = new Player
        {
            Id = Guid.NewGuid(),
            UserId = playerId,
            User = new User { Id = playerId, Name = "Player" },
            IsCreator = false,
            Game = game,
            GameId = game.Id,
        };

        // Creator submitted their category
        var category1 = new Category
        {
            Title = "Category 1",
            PlayerId = creator.Id,
            Player = creator,
            IsSubmitted = true,
        };

        // Player has not submitted their category
        var category2 = new Category
        {
            Title = "Category 2",
            PlayerId = player.Id,
            Player = player,
            IsSubmitted = false,
        };

        creator.Category = category1;
        player.Category = category2;
        game.Players = [creator, player];

        _getGame.Execute(gameCode).Returns(game);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, creatorId));

        Assert.Equal("All players must submit their categories before starting the game", exception.Message);
    }
}