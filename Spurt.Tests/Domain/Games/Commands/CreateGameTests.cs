using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;

namespace Spurt.Tests.Domain.Games.Commands;

public class CreateGameTests
{
    private readonly Guid _playerId = Guid.NewGuid();
    private readonly Player _player;
    private readonly IAddGame _addGame;
    private readonly CreateGame _createGame;

    public CreateGameTests()
    {
        _player = new Player { Id = _playerId, Name = "Test Player" };
        _addGame = Substitute.For<IAddGame>();
        var getPlayer = Substitute.For<IGetPlayer>();
        getPlayer.Execute(_playerId).Returns(_player);
        _createGame = new CreateGame(_addGame, getPlayer);
    }

    [Fact]
    public async Task Execute_WithValidPlayerId_SetsGameId()
    {
        var result = await _createGame.Execute(_playerId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Execute_WithValidPlayerId_SetsCorrectCodeLength()
    {
        var result = await _createGame.Execute(_playerId);

        Assert.Equal(6, result.Code.Length);
    }

    [Fact]
    public async Task Execute_WithValidPlayerId_GeneratesCodeWithExpectedCharacterSet()
    {
        var result = await _createGame.Execute(_playerId);

        Assert.Matches("^[A-HJ-NP-Z2-9]+$", result.Code);
    }

    [Fact]
    public async Task Execute_WithValidPlayerId_SetsCreatorCorrectly()
    {
        var result = await _createGame.Execute(_playerId);

        Assert.True(_player.IsCreator);
    }

    [Fact]
    public async Task Execute_WithValidPlayerId_AddsPlayerToCollection()
    {
        var result = await _createGame.Execute(_playerId);

        Assert.Contains(_player, result.Players);
    }

    [Fact]
    public async Task Execute_WithValidPlayerId_SetsCreatedAtToCurrentDate()
    {
        var result = await _createGame.Execute(_playerId);

        Assert.Equal(DateTime.UtcNow.Date, result.CreatedAt.Date);
    }

    [Fact]
    public async Task Execute_WithValidPlayerId_CallsAddGame()
    {
        var result = await _createGame.Execute(_playerId);

        await _addGame.Received(1).Execute(Arg.Is<Game>(g => g.Id == result.Id));
    }

    [Fact]
    public async Task Execute_WithInvalidPlayerId_ThrowsArgumentException()
    {
        var invalidPlayerId = Guid.NewGuid();
        var getPlayer = Substitute.For<IGetPlayer>();
        getPlayer.Execute(invalidPlayerId).Returns((Player?)null);
        var createGame = new CreateGame(_addGame, getPlayer);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => createGame.Execute(invalidPlayerId));

        Assert.Contains("Player not found", exception.Message);
        Assert.Equal("playerId", exception.ParamName);
    }

    [Fact]
    public async Task Execute_GeneratesUniqueCodesAcrossMultipleCalls()
    {
        var codes = new HashSet<string>();
        const int iterations = 20;

        for (var i = 0; i < iterations; i++)
        {
            var game = await _createGame.Execute(_playerId);
            codes.Add(game.Code);
        }

        Assert.Equal(iterations, codes.Count);
    }
}