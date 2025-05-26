using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Users;

namespace Spurt.Tests.Domain.Games.Commands;

public class CreateGameTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly User _user;
    private readonly IAddGame _addGame;
    private readonly CreateGame _createGame;
    private readonly string _username = "Test User";

    public CreateGameTests()
    {
        _user = new User { Id = _userId, Name = _username };
        _addGame = Substitute.For<IAddGame>();
        _addGame.Execute(Arg.Any<Game>()).Returns(callInfo =>
        {
            var game = callInfo.Arg<Game>();
            game.Id = Guid.NewGuid(); // Simulate database setting the ID
            return game;
        });
        var getUser = Substitute.For<IGetUser>();
        getUser.Execute(_userId).Returns(_user);
        _createGame = new CreateGame(_addGame, getUser);
    }

    [Fact]
    public async Task Execute_WithValidUserId_SetsGameId()
    {
        var result = await _createGame.Execute(_userId);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task Execute_WithValidUserId_SetsCorrectCodeLength()
    {
        var result = await _createGame.Execute(_userId);

        Assert.Equal(6, result.Code.Length);
    }

    [Fact]
    public async Task Execute_WithValidUserId_GeneratesCodeWithExpectedCharacterSet()
    {
        var result = await _createGame.Execute(_userId);

        Assert.Matches("^[A-HJ-NP-Z2-9]+$", result.Code);
    }

    [Fact]
    public async Task Execute_WithValidUserId_CreatesPlayerWithCreatorFlag()
    {
        var result = await _createGame.Execute(_userId);

        Assert.True(result.Players[0].IsCreator);
        Assert.Equal(_userId, result.Players[0].UserId);
    }

    [Fact]
    public async Task Execute_WithValidUserId_AddsPlayerToGame()
    {
        var result = await _createGame.Execute(_userId);

        Assert.Single(result.Players);
        Assert.Equal(_userId, result.Players[0].UserId);
    }

    [Fact]
    public async Task Execute_WithValidUserId_SetsCreatedAtToCurrentDate()
    {
        var result = await _createGame.Execute(_userId);

        Assert.Equal(DateTime.UtcNow.Date, result.CreatedAt.Date);
    }

    [Fact]
    public async Task Execute_WithValidUserId_CallsAddGame()
    {
        var result = await _createGame.Execute(_userId);

        await _addGame.Received(1).Execute(Arg.Is<Game>(g => g.Id == result.Id));
    }

    [Fact]
    public async Task Execute_WithInvalidUserId_ThrowsArgumentException()
    {
        var invalidUserId = Guid.NewGuid();
        var getUser = Substitute.For<IGetUser>();
        getUser.Execute(invalidUserId).Returns((User?)null);
        var createGame = new CreateGame(_addGame, getUser);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => createGame.Execute(invalidUserId));

        Assert.Contains("User not found", exception.Message);
        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public async Task Execute_GeneratesUniqueCodesAcrossMultipleCalls()
    {
        var codes = new HashSet<string>();
        const int iterations = 20;

        for (var i = 0; i < iterations; i++)
        {
            var game = await _createGame.Execute(_userId);
            codes.Add(game.Code);
        }

        Assert.Equal(iterations, codes.Count);
    }
}