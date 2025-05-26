using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Tests.Domain.Games.Commands;

public class JudgeAnswerTests
{
    private readonly IGetGame _getGame = Substitute.For<IGetGame>();
    private readonly IUpdateGame _updateGame = Substitute.For<IUpdateGame>();
    private readonly IGameHubNotificationService _notificationService = Substitute.For<IGameHubNotificationService>();
    private readonly JudgeAnswer _sut;

    public JudgeAnswerTests()
    {
        _sut = new JudgeAnswer(_getGame, _updateGame, _notificationService);
    }

    [Fact]
    public async Task Execute_WhenAnswerIsCorrect_AwardsPointsToPlayer()
    {
        // Arrange
        var gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var buzzedPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.BuzzerPressed,
        };

        var player1 = new Player
        {
            Id = buzzedPlayerId,
            User = new User { Name = "Player 1" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };

        var player2 = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Player 2" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var category1 = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = player1.Id,
            Player = player1,
            Clues = [],
        };
        player1.Category = category1;
        var category2 = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = player2.Id,
            Player = player2,
            Clues = [],
        };
        player2.Category = category2;
        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = category2.Id,
            Category = category2,
        };
        var clue2 = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 300,
            Answer = "Another Answer",
            Question = "Another Question",
            CategoryId = category2.Id,
            Category = category2,
        };
        category2.Clues = [clue, clue2];

        game.Players = [player1, player2];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;
        game.BuzzedPlayerId = buzzedPlayerId;
        game.BuzzedPlayer = player1;
        game.BuzzedTime = DateTime.UtcNow;

        // Mock return values
        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(args =>
        {
            // Ensure the mock returns the updated game state with the second clue still unanswered
            var updatedGame = (Game)args[0];
            return updatedGame;
        });

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId, true);

        // Assert
        Assert.Equal(GameState.InProgress, result.State);
        Assert.Null(result.BuzzedPlayerId);

        // Verify the buzzed player got the clue and points
        var updatedPlayer = result.Players.First(p => p.Id == buzzedPlayerId);
        Assert.Equal(200, updatedPlayer.GetScore());

        // Verify the buzzer state was reset
        Assert.Null(result.BuzzedPlayerId);
        Assert.Null(result.BuzzedPlayer);
        Assert.Null(result.BuzzedTime);

        // Verify the next player choosing is the one who buzzed
        Assert.Equal(buzzedPlayerId, result.CurrentChoosingPlayerId);

        // Verify game was updated and notification was sent
        await _updateGame.Received(1).Execute(Arg.Any<Game>());
        await _notificationService.Received(1).NotifyGameUpdated(Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_WhenAnswerIsIncorrect_DoesNotAwardPoints()
    {
        // Arrange
        var gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var buzzedPlayerId = Guid.NewGuid();
        var originalChoosingPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.BuzzerPressed,
            CurrentChoosingPlayerId = originalChoosingPlayerId,
        };

        var player1 = new Player
        {
            Id = buzzedPlayerId,
            User = new User { Name = "Player 1" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };

        var player2 = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Player 2" },
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
            Player = player2,
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

        game.Players = [player1, player2];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;
        game.BuzzedPlayerId = buzzedPlayerId;
        game.BuzzedPlayer = player1;
        game.BuzzedTime = DateTime.UtcNow;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(game);

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId, false);

        // Assert
        Assert.Equal(GameState.ClueSelected, result.State);

        // Verify the buzzed player didn't get points
        var updatedPlayer = result.Players.First(p => p.Id == buzzedPlayerId);
        Assert.Equal(0, updatedPlayer.GetScore());
        Assert.Empty(updatedPlayer.AnsweredClues);

        // Verify the buzzer state was reset
        Assert.Null(result.BuzzedPlayerId);
        Assert.Null(result.BuzzedPlayer);
        Assert.Null(result.BuzzedTime);

        // Verify the next player choosing remains the original
        Assert.Equal(originalChoosingPlayerId, result.CurrentChoosingPlayerId);

        // Verify game was updated and notification was sent
        await _updateGame.Received(1).Execute(Arg.Any<Game>());
        await _notificationService.Received(1).NotifyGameUpdated(Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_WhenPlayerIsNotClueOwner_ThrowsException()
    {
        // Arrange
        var gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var nonClueOwnerId = Guid.NewGuid();
        var buzzedPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.BuzzerPressed,
        };

        var player = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = clueOwnerId,
            Player = player,
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

        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;
        game.BuzzedPlayerId = buzzedPlayerId;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.Execute(gameCode, nonClueOwnerId, true));
    }

    [Fact]
    public async Task Execute_WhenGameStateIsNotBuzzerPressed_ThrowsException()
    {
        // Arrange
        var gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();

        var game = new Game
        {
            Code = gameCode,
            State = GameState.InProgress,
        };

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.Execute(gameCode, clueOwnerId, true));
    }

    [Fact]
    public async Task Execute_WhenAllCluesAreAnswered_TransitionsToFinishedState()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var buzzedPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.BuzzerPressed,
        };
        var player1 = new Player
        {
            Id = buzzedPlayerId,
            User = new User { Name = "Player 1" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var player2 = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Player 2" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var category1 = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = clueOwnerId,
            Player = player1,
        };
        player1.Category = category1;
        var category2 = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Test Category",
            PlayerId = clueOwnerId,
            Player = player2,
        };
        player2.Category = category2;
        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = category2.Id,
            Category = category2,
        };
        category2.Clues = [clue];
        player2.Category = category2;
        game.Players = [player1, player2];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;
        game.BuzzedPlayerId = buzzedPlayerId;
        game.BuzzedPlayer = player1;
        game.BuzzedTime = DateTime.UtcNow;
        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(game);

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId, true);

        // Assert
        Assert.Equal(GameState.Finished, result.State);
        Assert.True(clue.IsAnswered);
        var updatedPlayer = result.Players.First(p => p.Id == buzzedPlayerId);
        Assert.Equal(200, updatedPlayer.GetScore());
        await _updateGame.Received(1).Execute(Arg.Any<Game>());
        await _notificationService.Received(1).NotifyGameUpdated(Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_WhenCorrectAnswerAndOtherPlayersHaveClues_BuzzedPlayerBecomesChooser()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var buzzedPlayerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.BuzzerPressed,
        };

        var buzzedPlayer = new Player
        {
            Id = buzzedPlayerId,
            User = new User { Name = "Buzzed Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var buzzedPlayerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Buzzed Player Category",
            PlayerId = buzzedPlayerId,
            Player = buzzedPlayer,
            Clues = [],
        };
        buzzedPlayer.Category = buzzedPlayerCategory;

        var clueOwnerPlayer = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Clue Owner" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var clueOwnerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Clue Owner Category",
            PlayerId = clueOwnerId,
            Player = clueOwnerPlayer,
            Clues = [],
        };
        clueOwnerPlayer.Category = clueOwnerCategory;

        var otherPlayer = new Player
        {
            Id = otherPlayerId,
            User = new User { Name = "Other Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var otherPlayerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Other Player Category",
            PlayerId = otherPlayerId,
            Player = otherPlayer,
            Clues = [],
        };
        otherPlayer.Category = otherPlayerCategory;

        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = clueOwnerCategory.Id,
            Category = clueOwnerCategory,
        };
        clueOwnerCategory.Clues = [clue];

        var otherClue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 300,
            Answer = "Other Answer",
            Question = "Other Question",
            CategoryId = otherPlayerCategory.Id,
            Category = otherPlayerCategory,
        };
        otherPlayerCategory.Clues = [otherClue];

        game.Players = [buzzedPlayer, clueOwnerPlayer, otherPlayer];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;
        game.BuzzedPlayerId = buzzedPlayerId;
        game.BuzzedPlayer = buzzedPlayer;
        game.BuzzedTime = DateTime.UtcNow;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(args => (Game)args[0]);

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId, true);

        // Assert
        Assert.Equal(GameState.InProgress, result.State);
        Assert.Equal(buzzedPlayerId, result.CurrentChoosingPlayerId);
    }

    [Fact]
    public async Task Execute_WhenCorrectAnswerAndOnlyBuzzedPlayerHasClues_RandomOtherPlayerBecomesChooser()
    {
        // Arrange
        var gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var buzzedPlayerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.BuzzerPressed,
        };

        var buzzedPlayer = new Player
        {
            Id = buzzedPlayerId,
            User = new User { Name = "Buzzed Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var buzzedPlayerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Buzzed Player Category",
            PlayerId = buzzedPlayerId,
            Player = buzzedPlayer,
            Clues = [],
        };
        buzzedPlayer.Category = buzzedPlayerCategory;

        var clueOwnerPlayer = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Clue Owner" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var clueOwnerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Clue Owner Category",
            PlayerId = clueOwnerId,
            Player = clueOwnerPlayer,
            Clues = [],
        };
        clueOwnerPlayer.Category = clueOwnerCategory;

        var otherPlayer = new Player
        {
            Id = otherPlayerId,
            User = new User { Name = "Other Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var otherPlayerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Other Player Category",
            PlayerId = otherPlayerId,
            Player = otherPlayer,
            Clues = [],
        };
        otherPlayer.Category = otherPlayerCategory;

        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = clueOwnerCategory.Id,
            Category = clueOwnerCategory,
        };
        clueOwnerCategory.Clues = [clue];

        // Add an unanswered clue to the buzzed player's category only
        var buzzedPlayerClue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 300,
            Answer = "Buzzed Player Answer",
            Question = "Buzzed Player Question",
            CategoryId = buzzedPlayerCategory.Id,
            Category = buzzedPlayerCategory,
        };
        buzzedPlayerCategory.Clues = [buzzedPlayerClue];

        // Other player has no clues (all answered/none)
        otherPlayerCategory.Clues = [];

        game.Players = [buzzedPlayer, clueOwnerPlayer, otherPlayer];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;
        game.BuzzedPlayerId = buzzedPlayerId;
        game.BuzzedPlayer = buzzedPlayer;
        game.BuzzedTime = DateTime.UtcNow;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(args => (Game)args[0]);

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId, true);

        // Assert
        Assert.Equal(GameState.InProgress, result.State);
        Assert.NotEqual(buzzedPlayerId, result.CurrentChoosingPlayerId);
        Assert.True(result.CurrentChoosingPlayerId == clueOwnerId || result.CurrentChoosingPlayerId == otherPlayerId);
    }

    [Fact]
    public async Task Execute_WhenCorrectAnswerAndOnlyTwoPlayers_BuzzedPlayerBecomesChooserDespiteOnlyHavingOwnClues()
    {
        // Arrange
        const string gameCode = "ABCD";
        var clueOwnerId = Guid.NewGuid();
        var buzzedPlayerId = Guid.NewGuid();

        var game = new Game
        {
            Id = Guid.NewGuid(),
            Code = gameCode,
            State = GameState.BuzzerPressed,
        };

        var buzzedPlayer = new Player
        {
            Id = buzzedPlayerId,
            User = new User { Name = "Buzzed Player" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var buzzedPlayerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Buzzed Player Category",
            PlayerId = buzzedPlayerId,
            Player = buzzedPlayer,
            Clues = [],
        };
        buzzedPlayer.Category = buzzedPlayerCategory;

        var clueOwnerPlayer = new Player
        {
            Id = clueOwnerId,
            User = new User { Name = "Clue Owner" },
            UserId = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            AnsweredClues = [],
        };
        var clueOwnerCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Clue Owner Category",
            PlayerId = clueOwnerId,
            Player = clueOwnerPlayer,
            Clues = [],
        };
        clueOwnerPlayer.Category = clueOwnerCategory;

        var clue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 200,
            Answer = "Test Answer",
            Question = "Test Question",
            CategoryId = clueOwnerCategory.Id,
            Category = clueOwnerCategory,
        };
        clueOwnerCategory.Clues = [clue];

        // Add an unanswered clue to the buzzed player's category only
        var buzzedPlayerClue = new Clue
        {
            Id = Guid.NewGuid(),
            PointValue = 300,
            Answer = "Buzzed Player Answer",
            Question = "Buzzed Player Question",
            CategoryId = buzzedPlayerCategory.Id,
            Category = buzzedPlayerCategory,
        };
        buzzedPlayerCategory.Clues = [buzzedPlayerClue];

        game.Players = [buzzedPlayer, clueOwnerPlayer];
        game.SelectedClue = clue;
        game.SelectedClueId = clue.Id;
        game.BuzzedPlayerId = buzzedPlayerId;
        game.BuzzedPlayer = buzzedPlayer;
        game.BuzzedTime = DateTime.UtcNow;

        _getGame.Execute(gameCode, Arg.Any<bool>()).Returns(game);
        _updateGame.Execute(Arg.Any<Game>()).Returns(args => (Game)args[0]);

        // Act
        var result = await _sut.Execute(gameCode, clueOwnerId, true);

        // Assert
        Assert.Equal(GameState.InProgress, result.State);
        Assert.Equal(clueOwnerId, result.CurrentChoosingPlayerId); // Should choose the other player
    }
}