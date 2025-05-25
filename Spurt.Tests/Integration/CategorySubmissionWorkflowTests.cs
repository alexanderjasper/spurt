using Microsoft.Extensions.DependencyInjection;
using Spurt.Domain.Categories;
using Spurt.Domain.Categories.Commands;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Users.Commands;

namespace Spurt.Tests.Integration;

public class CategorySubmissionWorkflowTests
{
    private readonly TestDbContextFixture _fixture = new();

    [Fact]
    public async Task CategorySubmission_ThenStartGame_Success()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();

        var game = await helper.CreateGame(2);
        var creatorPlayer = game.Players.Single(p => p.IsCreator);
        var startedGame = await startGame.Execute(game.Code, creatorPlayer.UserId);
        Assert.Equal(GameState.InProgress, startedGame.State);

        // Verify the game state and that the creator is the first player to choose
        Assert.Equal(creatorPlayer.Id, startedGame.CurrentChoosingPlayerId);
    }

    [Fact]
    public async Task CategorySubmission_NotAllCategoriesSubmitted_CannotStartGame()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from the test environment's service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var joinGame = testEnv.ServiceProvider.GetRequiredService<JoinGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();

        // Step 1: Set up a game with two players
        var user = await registerUser.Execute("Creator");
        var user2 = await registerUser.Execute("Player 2");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();
        game = await joinGame.Execute(game.Code, user2.Id);
        var player2 = game.Players.Single(x => x.UserId == user2.Id);

        // Step 2: Only the creator submits a category
        var category1 = new Category
        {
            Title = "",
            PlayerId = player.Id,
            Clues = [],
            Player = player,
        };
        foreach (var pointValue in new[] { 100, 200, 300, 400, 500 })
            category1.Clues.Add(new Clue
            {
                Answer = "",
                Question = "",
                PointValue = pointValue,
                CategoryId = category1.Id,
                Category = category1,
            });
        await saveCategory.Execute(category1, true);

        // Step 3: Try to start the game, should fail
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await startGame.Execute(game.Code, user.Id));

        Assert.Contains("All players must submit", exception.Message);
    }

    [Fact]
    public async Task CategorySubmission_NotEnoughPlayers_CannotStartGame()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from the test environment's service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();

        // Step 1: Set up a game with only one player
        var user = await registerUser.Execute("Creator");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();

        // Step 2: Creator submits a category
        var category1 = new Category
        {
            Title = "",
            PlayerId = player.Id,
            Clues = [],
            Player = player,
        };
        foreach (var pointValue in new[] { 100, 200, 300, 400, 500 })
            category1.Clues.Add(new Clue
            {
                Answer = "",
                Question = "",
                PointValue = pointValue,
                CategoryId = category1.Id,
                Category = category1,
            });
        await saveCategory.Execute(category1, true);

        // Step 3: Try to start the game, should fail
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await startGame.Execute(game.Code, user.Id));

        Assert.Contains("At least", exception.Message);
    }

    [Fact]
    public async Task CategorySubmission_InsufficientClues_CannotSubmit()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from the test environment's service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();

        // Step 1: Set up a game with only one player
        var user = await registerUser.Execute("Creator");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();

        // Step 2: Creator submits a category
        var category1 = new Category
        {
            Title = "",
            PlayerId = player.Id,
            Clues = [],
            Player = player,
        };
        foreach (var pointValue in new[] { 100, 200, 300, 400 })
            category1.Clues.Add(new Clue
            {
                Answer = "",
                Question = "",
                PointValue = pointValue,
                CategoryId = category1.Id,
                Category = category1,
            });

        // Step 3: Try to submit, should fail with ArgumentException since validation happens before database access
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await saveCategory.Execute(category1, true));

        Assert.Contains("must have exactly 5 clues", exception.Message);
    }

    [Fact]
    public async Task CategorySubmission_InvalidPointValues_CannotSubmit()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();

        // Get real implementations from the test environment's service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();

        // Step 1: Set up a game with only one player
        var user = await registerUser.Execute("Creator");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();

        // Step 2: Creator submits a category
        var category1 = new Category
        {
            Title = "",
            PlayerId = player.Id,
            Clues = [],
            Player = player,
        };
        foreach (var pointValue in new[] { 100, 250, 300, 400, 500 })
            category1.Clues.Add(new Clue
            {
                Answer = "",
                Question = "",
                PointValue = pointValue,
                CategoryId = category1.Id,
                Category = category1,
            });

        // Step 3: Try to submit, should fail with ArgumentException since validation happens before database access
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await saveCategory.Execute(category1, true));

        Assert.Contains("Clue point values must be 100, 200, 300", exception.Message);
    }
}