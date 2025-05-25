using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Tests.Domain.Games.Commands;

public class SelectClueTests
{
    private readonly IGetGame _getGame = Substitute.For<IGetGame>();
    private readonly IGetClue _getClue = Substitute.For<IGetClue>();
    private readonly IUpdateGame _updateGame = Substitute.For<IUpdateGame>();
    private readonly IGameHubNotificationService _notificationService = Substitute.For<IGameHubNotificationService>();
    private readonly SelectClue _sut;

    public SelectClueTests()
    {
        _sut = new SelectClue(_getGame, _getClue, _updateGame, _notificationService);
    }

    [Fact]
    public async Task Execute_WhenGameDoesNotExist_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueId = Guid.NewGuid();

        _getGame.Execute(gameCode).Returns(Task.FromResult<Game?>(null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, clueId));

        Assert.Equal("Spillet findes ikke", exception.Message);
    }

    [Fact]
    public async Task Execute_WhenGameNotInProgress_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
            State = GameState.WaitingForCategories,
        };

        _getGame.Execute(gameCode).Returns(game);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, clueId));

        Assert.Equal("Spillet er ikke i gang", exception.Message);
    }

    [Fact]
    public async Task Execute_WhenClueDoesNotExist_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
            State = GameState.InProgress,
        };

        _getGame.Execute(gameCode).Returns(game);
        _getClue.Execute(clueId).Returns(Task.FromResult<Clue?>(null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, clueId));

        Assert.Equal("Ledetråden er allerede besvaret", exception.Message);
    }

    [Fact]
    public async Task Execute_WhenClueIsAlreadyAnswered_ThrowsException()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
            State = GameState.InProgress,
        };

        var player = new Player
        {
            Id = Guid.NewGuid(),
            User = new User { Name = "Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
        };
        var player2 = new Player
        {
            Id = Guid.NewGuid(),
            User = new User { Name = "Player 2" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
        };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = player.Id,
            Player = player,
        };
        var clue = new Clue
        {
            Id = clueId,
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = category.Id,
            Category = category,
            AnsweredByPlayer = player2,
            AnsweredByPlayerId = player2.Id,
        };

        _getGame.Execute(gameCode).Returns(game);
        _getClue.Execute(clueId).Returns(clue);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, clueId));

        Assert.Equal("Ledetråden er allerede besvaret", exception.Message);
    }

    [Fact]
    public async Task Execute_WhenClueIsValid_SelectsClue()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
            State = GameState.InProgress,
        };

        var player = new Player
        {
            Id = Guid.NewGuid(),
            User = new User { Name = "Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = player.Id,
            Player = player,
        };

        var clue = new Clue
        {
            Id = clueId,
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = category.Id,
            Category = category,
        };

        _getGame.Execute(gameCode).Returns(game);
        _getClue.Execute(clueId).Returns(clue);
        _updateGame.Execute(Arg.Any<Game>()).Returns(game);

        // Act
        var result = await _sut.Execute(gameCode, clueId);

        // Assert
        Assert.Equal(GameState.ClueSelected, result.State);
        Assert.Equal(clueId, result.SelectedClueId);
        Assert.Equal(clue, result.SelectedClue);

        await _updateGame.Received(1).Execute(Arg.Any<Game>());
        await _notificationService.Received(1).NotifyGameUpdated(Arg.Any<string>());
    }
}