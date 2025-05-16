using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Players.Commands;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Spurt.Tests.Integration;

public class JoinGameWorkflowTests
{
    // Real implementations
    private readonly RegisterPlayer _registerPlayer;
    private readonly CreateGame _createGame;
    private readonly JoinGame _joinGame;

    // Mocked data layer
    private readonly IAddPlayer _addPlayer;
    private readonly IAddGame _addGame;
    private readonly IUpdateGame _updateGame;
    private readonly IGetPlayer _getPlayer;
    private readonly IGetGame _getGame;
    private readonly IHubContext<GameHub> _hubContext;

    // Test data
    private readonly Guid _player1Id = Guid.NewGuid();
    private readonly Guid _player2Id = Guid.NewGuid();
    private readonly Player _player1;
    private readonly Player _player2;
    private readonly Game _game;
    private readonly string _gameCode = "ABC123";

    public JoinGameWorkflowTests()
    {
        // Initialize test data
        _player1 = new Player { Id = _player1Id, Name = "Player 1", IsCreator = true };
        _player2 = new Player { Id = _player2Id, Name = "Player 2" };

        _game = new Game
        {
            Id = Guid.NewGuid(),
            Code = _gameCode,
            Players = [_player1],
        };

        // Configure mocks
        _addPlayer = Substitute.For<IAddPlayer>();
        _addGame = Substitute.For<IAddGame>();
        _updateGame = Substitute.For<IUpdateGame>();

        _getPlayer = Substitute.For<IGetPlayer>();
        _getPlayer.Execute(_player1Id).Returns(_player1);
        _getPlayer.Execute(_player2Id).Returns(_player2);

        _getGame = Substitute.For<IGetGame>();
        _getGame.Execute(_gameCode).Returns(_game);

        _hubContext = Substitute.For<IHubContext<GameHub>>();

        // Create real implementations with mocked dependencies
        _registerPlayer = new RegisterPlayer(_addPlayer);
        _createGame = new CreateGame(_addGame, _getPlayer);
        _joinGame = new JoinGame(_getGame, _getPlayer, _updateGame, _hubContext);
    }

    [Fact]
    public async Task CompleteJoinGameWorkflow_Success()
    {
        // Step 1: Register two players
        var player1 = await _registerPlayer.Execute("Player 1");
        var player2 = await _registerPlayer.Execute("Player 2");

        // Update mock to return our players
        _getPlayer.Execute(player1.Id).Returns(player1);
        _getPlayer.Execute(player2.Id).Returns(player2);

        // Step 2: First player creates a game
        var game = await _createGame.Execute(player1.Id);

        // Update mock to return the created game
        var gameWithPlayers = new Game
        {
            Id = game.Id,
            Code = game.Code,
            Players = [player1],
        };
        _getGame.Execute(game.Code).Returns(gameWithPlayers);

        // Step 3: Second player joins the game
        var updatedGame = await _joinGame.Execute(game.Code, player2.Id);

        // Verify join game result
        Assert.NotNull(updatedGame);
        Assert.Equal(2, updatedGame.Players.Count);
        Assert.Contains(player1, updatedGame.Players);
        Assert.Contains(player2, updatedGame.Players);

        // Verify game properties
        Assert.True(player1.IsCreator);
        Assert.False(player2.IsCreator);

        // Verify UpdateGame was called to persist changes
        await _updateGame.Received(1).Execute(Arg.Any<Game>());
    }

    [Fact]
    public async Task JoinGameWorkflow_PlayerAlreadyInGame_ReturnsGameWithoutChange()
    {
        // Add player2 to the game
        _game.Players.Add(_player2);

        // Player2 tries to join the game again
        var result = await _joinGame.Execute(_gameCode, _player2Id);

        // Verify player2 is still in the game (only once)
        Assert.Equal(2, result.Players.Count);
        Assert.Contains(_player1, result.Players);
        Assert.Contains(_player2, result.Players);

        // Verify update was not called
        await _updateGame.DidNotReceive().Execute(Arg.Any<Game>());
    }

    [Fact]
    public async Task JoinGameWorkflow_InvalidGameCode_ThrowsException()
    {
        const string invalidGameCode = "INVALID";
        _getGame.Execute(invalidGameCode).Returns((Game?)null);

        // Try to join a non-existent game
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _joinGame.Execute(invalidGameCode, _player2Id));

        Assert.Contains("Game not found", exception.Message);
    }
}