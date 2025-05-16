using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Spurt.Tests.Domain.Games.Commands;

public class JoinGameTests
{
    private readonly Guid _playerId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();
    private const string GameCode = "ABC123";
    private readonly Player _player;
    private readonly Player _creator;
    private readonly Game _game;
    private readonly IGetGame _getGame;
    private readonly IGetPlayer _getPlayer;
    private readonly IUpdateGame _updateGame;
    private readonly JoinGame _joinGame;

    public JoinGameTests()
    {
        _player = new Player { Id = _playerId, Name = "Test Player" };
        _creator = new Player { Id = _creatorId, Name = "Creator" };

        _game = new Game
        {
            Id = Guid.NewGuid(),
            Code = GameCode,
            Creator = _creator,
            CreatorId = _creatorId,
            Players = new List<Player> { _creator },
        };

        _getGame = Substitute.For<IGetGame>();
        _getGame.Execute(GameCode).Returns(_game);

        _getPlayer = Substitute.For<IGetPlayer>();
        _getPlayer.Execute(_playerId).Returns(_player);

        _updateGame = Substitute.For<IUpdateGame>();

        _joinGame = new JoinGame(_getGame, _getPlayer, _updateGame);
    }

    [Fact]
    public async Task Execute_WithValidGameCodeAndPlayerId_AddsPlayerToGame()
    {
        var result = await _joinGame.Execute(GameCode, _playerId);

        Assert.Contains(_player, result.Players);
    }

    [Fact]
    public async Task Execute_WithValidGameCodeAndPlayerId_CallsUpdateGame()
    {
        var result = await _joinGame.Execute(GameCode, _playerId);

        await _updateGame.Received(1).Execute(Arg.Is<Game>(g => g.Id == _game.Id));
    }

    [Fact]
    public async Task Execute_WithPlayerAlreadyInGame_ReturnsGameWithoutAddingPlayerAgain()
    {
        _game.Players.Add(_player);

        var result = await _joinGame.Execute(GameCode, _playerId);

        Assert.Equal(2, result.Players.Count);
        await _updateGame.DidNotReceive().Execute(Arg.Any<Game>());
    }

    [Fact]
    public async Task Execute_WithInvalidGameCode_ThrowsArgumentException()
    {
        const string invalidGameCode = "INVALID";
        _getGame.Execute(invalidGameCode).Returns((Game?)null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _joinGame.Execute(invalidGameCode, _playerId));

        Assert.Contains("Game not found", exception.Message);
        Assert.Equal("gameCode", exception.ParamName);
    }

    [Fact]
    public async Task Execute_WithInvalidPlayerId_ThrowsArgumentException()
    {
        var invalidPlayerId = Guid.NewGuid();
        _getPlayer.Execute(invalidPlayerId).Returns((Player?)null);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _joinGame.Execute(GameCode, invalidPlayerId));

        Assert.Contains("Player not found", exception.Message);
        Assert.Equal("playerId", exception.ParamName);
    }
}