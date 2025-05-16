using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;
using Spurt.Domain.Users.Commands;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Spurt.Tests.Integration;

public class JoinGameWorkflowTests
{
    // Real implementations
    private readonly RegisterUser _registerUser;
    private readonly CreateGame _createGame;
    private readonly JoinGame _joinGame;

    // Mocked data layer
    private readonly IAddUser _addUser;
    private readonly IAddPlayer _addPlayer;
    private readonly IAddGame _addGame;
    private readonly IGetUser _getUser;
    private readonly IGetGame _getGame;
    private readonly IHubContext<GameHub> _hubContext;

    // Test data
    private readonly Guid _user1Id = Guid.NewGuid();
    private readonly Guid _user2Id = Guid.NewGuid();
    private readonly User _user1;
    private readonly User _user2;
    private readonly Player _player1;
    private readonly Game _game;
    private readonly string _gameCode = "ABC123";

    public JoinGameWorkflowTests()
    {
        // Initialize test data
        _user1 = new User { Id = _user1Id, Name = "User 1" };
        _user2 = new User { Id = _user2Id, Name = "User 2" };

        _game = new Game
        {
            Id = Guid.NewGuid(),
            Code = _gameCode,
            Players = [],
        };

        _player1 = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _user1Id,
            User = _user1,
            GameId = _game.Id,
            Game = _game,
            IsCreator = true,
        };

        _game.Players.Add(_player1);

        // Configure mocks
        _addUser = Substitute.For<IAddUser>();
        _addPlayer = Substitute.For<IAddPlayer>();
        _addGame = Substitute.For<IAddGame>();

        _getUser = Substitute.For<IGetUser>();
        _getUser.Execute(_user1Id).Returns(_user1);
        _getUser.Execute(_user2Id).Returns(_user2);

        _getGame = Substitute.For<IGetGame>();
        _getGame.Execute(_gameCode).Returns(_game);

        _hubContext = Substitute.For<IHubContext<GameHub>>();

        // Create real implementations with mocked dependencies
        _registerUser = new RegisterUser(_addUser);
        _createGame = new CreateGame(_addGame, _getUser);
        _joinGame = new JoinGame(_getGame, _getUser, _addPlayer, _hubContext);
    }

    [Fact]
    public async Task CompleteJoinGameWorkflow_Success()
    {
        // Step 1: Register two users
        var user1 = await _registerUser.Execute("User 1");
        var user2 = await _registerUser.Execute("User 2");

        // Update mock to return our users
        _getUser.Execute(user1.Id).Returns(user1);
        _getUser.Execute(user2.Id).Returns(user2);

        // Step 2: First user creates a game
        var game = await _createGame.Execute(user1.Id);

        // Update mock to return the created game with the player
        var gameWithPlayers = new Game
        {
            Id = game.Id,
            Code = game.Code,
            Players = new List<Player> { game.Players[0] },
        };
        _getGame.Execute(game.Code).Returns(gameWithPlayers);

        // Step 3: Second user joins the game
        var updatedGame = await _joinGame.Execute(game.Code, user2.Id);

        // Verify AddPlayer was called for user2
        await _addPlayer.Received(1).Execute(Arg.Is<Player>(p =>
            p.UserId == user2.Id && !p.IsCreator));

        // Verify join game result
        Assert.NotNull(updatedGame);
        // Player is added to the database and to the game.Players collection
        Assert.Equal(2, updatedGame.Players.Count);
        Assert.Single(updatedGame.Players, p => p.UserId == user1.Id && p.IsCreator);
        Assert.Single(updatedGame.Players, p => p.UserId == user2.Id && !p.IsCreator);
    }

    [Fact]
    public async Task JoinGameWorkflow_UserAlreadyHasPlayerInGame_ReturnsGameWithoutChange()
    {
        // Add a player for user2 to the game
        var player2 = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _user2Id,
            User = _user2,
            GameId = _game.Id,
            Game = _game,
        };
        _game.Players.Add(player2);

        // User2 tries to join the game again
        var result = await _joinGame.Execute(_gameCode, _user2Id);

        // Verify both players are still in the game (only once each)
        Assert.Equal(2, result.Players.Count);
        Assert.Single(result.Players, p => p.UserId == _user1Id);
        Assert.Single(result.Players, p => p.UserId == _user2Id);

        // Verify addPlayer was not called
        await _addPlayer.DidNotReceive().Execute(Arg.Any<Player>());
    }

    [Fact]
    public async Task JoinGameWorkflow_InvalidGameCode_ThrowsException()
    {
        const string invalidGameCode = "INVALID";
        _getGame.Execute(invalidGameCode).Returns((Game?)null);

        // Try to join a non-existent game
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _joinGame.Execute(invalidGameCode, _user2Id));

        Assert.Contains("Game not found", exception.Message);
    }
}