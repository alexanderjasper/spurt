using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Players.Commands;

namespace Spurt.Tests.Integration;

public class SignupAndGameCreationTests
{
    // Real implementations
    private readonly IRegisterPlayer _registerPlayer;
    private readonly ICreateGame _createGame;

    // Mocked data layer
    private readonly IAddPlayer _addPlayer;
    private readonly IAddGame _addGame;
    private readonly IGetPlayer _getPlayer;

    private readonly Player _testPlayer;
    private readonly Guid _playerId = Guid.NewGuid();

    public SignupAndGameCreationTests()
    {
        _addPlayer = Substitute.For<IAddPlayer>();
        _addGame = Substitute.For<IAddGame>();
        _getPlayer = Substitute.For<IGetPlayer>();

        _testPlayer = new Player
        {
            Id = _playerId,
            Name = "Test Player",
        };

        // Configure data layer mocks
        _getPlayer.Execute(_playerId).Returns(_testPlayer);

        // Create real implementations with mocked dependencies
        _registerPlayer = new RegisterPlayer(_addPlayer);
        _createGame = new CreateGame(_addGame, _getPlayer);
    }

    [Fact]
    public async Task CompleteUserJourney_RegisterAndCreateGame_Success()
    {
        // Step 1: Register a new player
        var playerName = "Test Player";
        var player = await _registerPlayer.Execute(playerName);

        // Verify player was created with the correct name
        Assert.NotNull(player);
        Assert.Equal(playerName, player.Name);

        // Verify AddPlayer was called with the correct player
        await _addPlayer.Received(1).Execute(Arg.Is<Player>(p => p.Name == playerName));

        // Configure GetPlayer mock to return the newly registered player
        // Note: In a real scenario, the player ID is assigned by the database
        _getPlayer.Execute(player.Id).Returns(player);

        // Step 2: Create a new game with the player
        var game = await _createGame.Execute(player.Id);

        // Verify AddGame was called 
        await _addGame.Received(1).Execute(Arg.Any<Game>());

        // Verify game properties
        Assert.NotNull(game);
        Assert.Equal(6, game.Code.Length); // Game code should be 6 characters
        Assert.True(player.IsCreator);

        // Verify player is added to game's players list
        Assert.Contains(player, game.Players);
    }
}