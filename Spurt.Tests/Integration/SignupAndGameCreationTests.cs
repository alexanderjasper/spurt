using Microsoft.Extensions.DependencyInjection;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Users.Commands;

namespace Spurt.Tests.Integration;

public class SignupAndGameCreationTests
{
    private readonly TestDbContextFixture _fixture = new();

    [Fact]
    public async Task CompleteUserJourney_RegisterAndCreateGame_Success()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from the test environment's service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();

        // Step 1: Register a new user
        var userName = "Test User";
        var user = await registerUser.Execute(userName);

        // Verify user was created with the correct name
        Assert.NotNull(user);
        Assert.Equal(userName, user.Name);

        // Verify the user exists in the database
        var dbUser = await testEnv.DbContext.Users.FindAsync(user.Id);
        Assert.NotNull(dbUser);
        Assert.Equal(userName, dbUser.Name);

        // Step 2: Create a new game with the user
        var game = await createGame.Execute(user.Id);

        // Verify the game was created in the database
        var dbGame = await testEnv.DbContext.Games.FindAsync(game.Id);
        Assert.NotNull(dbGame);
        Assert.Equal(game.Code, dbGame.Code);

        // Verify a Player was created for the User
        Assert.Single(game.Players);
        Assert.Equal(user.Id, game.Players[0].UserId);
        Assert.True(game.Players[0].IsCreator);

        // Verify game properties
        Assert.NotNull(game);
        Assert.Equal(6, game.Code.Length); // Game code should be 6 characters
    }
}