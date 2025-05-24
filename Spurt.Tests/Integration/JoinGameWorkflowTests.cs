using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Users.Commands;

// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Spurt.Tests.Integration;

public class JoinGameWorkflowTests
{
    private readonly TestDbContextFixture _fixture = new();

    [Fact]
    public async Task CompleteJoinGameWorkflow_Success()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var joinGame = testEnv.ServiceProvider.GetRequiredService<JoinGame>();
        var gameHubNotificationService = testEnv.ServiceProvider.GetRequiredService<IGameHubNotificationService>();

        // Clear any previous notification calls
        gameHubNotificationService.ClearReceivedCalls();

        // Step 1: Register two users
        var user1 = await registerUser.Execute("User 1");
        var user2 = await registerUser.Execute("User 2");

        // Step 2: First user creates a game
        var game = await createGame.Execute(user1.Id);

        // Step 3: Second user joins the game
        var updatedGame = await joinGame.Execute(game.Code, user2.Id);

        // Verify join game result
        Assert.NotNull(updatedGame);
        // Player is added to the database and to the game.Players collection
        Assert.Equal(2, updatedGame.Players.Count);
        Assert.Single(updatedGame.Players, p => p.UserId == user1.Id && p.IsCreator);
        Assert.Single(updatedGame.Players, p => p.UserId == user2.Id && !p.IsCreator);

        // Verify the notification service was called
        await gameHubNotificationService.Received(1).NotifyGameUpdated(Arg.Any<Game>());
    }

    [Fact]
    public async Task JoinGameWorkflow_UserAlreadyHasPlayerInGame_ReturnsGameWithoutChange()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var joinGame = testEnv.ServiceProvider.GetRequiredService<JoinGame>();
        var gameHubNotificationService = testEnv.ServiceProvider.GetRequiredService<IGameHubNotificationService>();

        // Setup: Create a user and a game, and have the user join
        var user1 = await registerUser.Execute("User 1");
        var user2 = await registerUser.Execute("User 2");
        var game = await createGame.Execute(user1.Id);

        // User2 joins the game
        var gameAfterFirstJoin = await joinGame.Execute(game.Code, user2.Id);

        // Reset the notification service call count
        gameHubNotificationService.ClearReceivedCalls();

        // User2 tries to join the game again
        var gameAfterSecondJoin = await joinGame.Execute(game.Code, user2.Id);

        // Verify both players are still in the game (only once each)
        Assert.Equal(2, gameAfterSecondJoin.Players.Count);
        Assert.Single(gameAfterSecondJoin.Players, p => p.UserId == user1.Id);
        Assert.Single(gameAfterSecondJoin.Players, p => p.UserId == user2.Id);

        // Verify notification was not called again
        await gameHubNotificationService.DidNotReceive().NotifyGameUpdated(Arg.Any<Game>());
    }

    [Fact]
    public async Task JoinGameWorkflow_InvalidGameCode_ThrowsException()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var joinGame = testEnv.ServiceProvider.GetRequiredService<JoinGame>();

        // Create a user but no game
        var user = await registerUser.Execute("User 2");

        const string invalidGameCode = "INVALID";

        // Try to join a non-existent game
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            joinGame.Execute(invalidGameCode, user.Id));

        Assert.Contains("Game not found", exception.Message);
    }
}