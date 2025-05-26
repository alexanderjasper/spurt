using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Tests.Domain.Games.Commands;

public class NoOneCanAnswerTests
{
    private readonly IGetGame _getGame = Substitute.For<IGetGame>();
    private readonly IUpdateGame _updateGame = Substitute.For<IUpdateGame>();
    private readonly IGameHubNotificationService _notificationService = Substitute.For<IGameHubNotificationService>();
    private readonly NoOneCanAnswer _sut;

    public NoOneCanAnswerTests()
    {
        _sut = new NoOneCanAnswer(_getGame, _updateGame, _notificationService);
    }

    [Fact]
    public async Task Execute_WhenNoOneCanAnswer_AssignsPenaltyToClueOwner()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.ClueSelected,
        };
        var clueOwner = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Clue Owner" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = clueOwnerId,
            Player = clueOwner,
        };
        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = category.Id,
            Category = category,
        };

        var otherPlayer = new Player
        {
            Id = otherPlayerId,
            User = new User { Name = "Other Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var otherCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Other Category",
            PlayerId = otherPlayerId,
            Player = otherPlayer,
            Clues = [],
        };
        var otherClue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 100,
            Answer = "Other Answer",
            Question = "Other Question",
            CategoryId = otherCategory.Id,
            Category = otherCategory,
        };

        category.Clues = [clue];
        otherCategory.Clues = [otherClue];
        clueOwner.Category = category;
        otherPlayer.Category = otherCategory;

        game.Players = [clueOwner, otherPlayer];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(game);

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId);

        // Assert
        Assert.True(clue.NoOneCouldAnswer);
        Assert.Equal(clueOwnerId, clue.AnsweredByPlayerId);
        Assert.Contains(clue, clueOwner.AnsweredClues);
        Assert.Equal(-200, clueOwner.GetScore()); // Penalty points
        Assert.Equal(GameState.InProgress, result.State);
        Assert.Null(result.SelectedClue);
        Assert.Null(result.SelectedClueId);
        await _updateGame.Received(1).Execute(Arg.Any<Game>());
        await _notificationService.Received(1).NotifyGameUpdated(gameCode);
    }

    [Fact]
    public async Task Execute_WhenAllCluesAnswered_TransitionsToFinishedState()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.ClueSelected,
        };

        var clueOwner = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Clue Owner" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = clueOwnerId,
            Player = clueOwner,
        };
        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = category.Id,
            Category = category,
        };

        category.Clues = [clue];
        clueOwner.Category = category;

        game.Players = [clueOwner];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(game);

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId);

        // Assert
        Assert.Equal(GameState.Finished, result.State);
    }

    [Fact]
    public async Task Execute_WhenNotClueOwner_ThrowsException()
    {
        // Arrange
        var gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.ClueSelected,
        };

        var clueOwner = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Clue Owner" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
        };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = clueOwnerId,
            Player = clueOwner,
        };
        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = category.Id,
            Category = category,
        };

        game.Players = [clueOwner];
        game.SelectedClue = clue;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, otherPlayerId));
    }

    [Theory]
    [InlineData(GameState.WaitingForCategories)]
    [InlineData(GameState.InProgress)]
    [InlineData(GameState.BuzzerPressed)]
    [InlineData(GameState.Finished)]
    public async Task Execute_WhenGameStateInvalid_ThrowsException(GameState invalidState)
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = invalidState,
        };
        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Execute(gameCode, clueOwnerId));
    }
}