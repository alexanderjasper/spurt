using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Spurt.Tests.Domain.Games.Commands;

public class JoinGameTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _creatorUserId = Guid.NewGuid();
    private const string GameCode = "ABC123";
    private readonly User _user;
    private readonly User _creatorUser;
    private readonly Player _creatorPlayer;
    private readonly Game _game;
    private readonly IGetGame _getGame;
    private readonly IGetUser _getUser;
    private readonly IAddPlayer _addPlayer;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly JoinGame _joinGame;

    public JoinGameTests()
    {
        _user = new User { Id = _userId, Name = "Test User" };
        _creatorUser = new User { Id = _creatorUserId, Name = "Creator User" };

        _game = new Game
        {
            Id = Guid.NewGuid(),
            Code = GameCode,
            Players = [],
        };

        _creatorPlayer = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _creatorUserId,
            User = _creatorUser,
            GameId = _game.Id,
            Game = _game,
            IsCreator = true,
        };

        _game.Players.Add(_creatorPlayer);

        _getGame = Substitute.For<IGetGame>();
        _getGame.Execute(GameCode).Returns(_game);

        _getUser = Substitute.For<IGetUser>();
        _getUser.Execute(_userId).Returns(_user);
        _getUser.Execute(_creatorUserId).Returns(_creatorUser);

        _addPlayer = Substitute.For<IAddPlayer>();
        _hubContext = Substitute.For<IHubContext<GameHub>>();

        _joinGame = new JoinGame(_getGame, _getUser, _addPlayer, _hubContext);
    }

    [Fact]
    public async Task Execute_WithValidGameCodeAndUserId_CreatesPlayerAndAddsToDB()
    {
        var result = await _joinGame.Execute(GameCode, _userId);

        await _addPlayer.Received(1).Execute(Arg.Is<Player>(p =>
            p.UserId == _userId));

        Assert.Equal(2, result.Players.Count);
    }

    [Fact]
    public async Task Execute_WithUserAlreadyHavingPlayerInGame_ReturnsGameWithoutAddingPlayerAgain()
    {
        var existingPlayer = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            User = _user,
            GameId = _game.Id,
            Game = _game,
        };
        _game.Players.Add(existingPlayer);

        var result = await _joinGame.Execute(GameCode, _userId);

        Assert.Equal(2, result.Players.Count);
        await _addPlayer.DidNotReceive().Execute(Arg.Any<Player>());
    }

    [Fact]
    public async Task Execute_WithInvalidGameCode_ThrowsArgumentException()
    {
        const string invalidGameCode = "INVALID";
        _getGame.Execute(invalidGameCode).Returns((Game?)null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _joinGame.Execute(invalidGameCode, _userId));

        Assert.Contains("Game not found", exception.Message);
        Assert.Equal("gameCode", exception.ParamName);
    }

    [Fact]
    public async Task Execute_WithInvalidUserId_ThrowsArgumentException()
    {
        var invalidUserId = Guid.NewGuid();
        _getUser.Execute(invalidUserId).Returns((User?)null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _joinGame.Execute(GameCode, invalidUserId));

        Assert.Contains("User not found", exception.Message);
        Assert.Equal("userId", exception.ParamName);
    }
}