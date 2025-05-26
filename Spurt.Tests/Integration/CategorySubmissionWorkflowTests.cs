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
        using var testEnv = _fixture.CreateTestEnvironment();
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var joinGame = testEnv.ServiceProvider.GetRequiredService<JoinGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();

        // Set up a game with two players
        var user = await registerUser.Execute("Creator");
        var user2 = await registerUser.Execute("Player 2");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();
        game = await joinGame.Execute(game.Code, user2.Id);
        var player2 = game.Players.Single(x => x.UserId == user2.Id);
        player.Category = new Category
        {
            Player = player,
            PlayerId = player.Id,
            Title = "Test Category",
            Clues = [],
        };
        foreach (var pointValue in new[] { 100, 200, 300, 400, 500 })
            player.Category.Clues.Add(new Clue
            {
                Question = $"Question for {pointValue}",
                Answer = $"Answer for {pointValue}",
                PointValue = pointValue,
                CategoryId = player.Category.Id,
                Category = player.Category,
            });
        await saveCategory.Execute(player.Category, true);

        // Try to start the game, should fail
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await startGame.Execute(game.Code, user.Id));
        Assert.Contains("All players must submit", exception.Message);
    }

    [Fact]
    public async Task CategorySubmission_NotEnoughPlayers_CannotStartGame()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();

        // Set up a game with only one player
        var user = await registerUser.Execute("Creator");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();
        player.Category = new Category
        {
            Player = player,
            PlayerId = player.Id,
            Title = "Test Category",
            Clues = [],
        };
        foreach (var pointValue in new[] { 100, 200, 300, 400, 500 })
            player.Category.Clues.Add(new Clue
            {
                Question = $"Question for {pointValue}",
                Answer = $"Answer for {pointValue}",
                PointValue = pointValue,
                CategoryId = player.Category.Id,
                Category = player.Category,
            });
        await saveCategory.Execute(player.Category, true);

        // Try to start the game, should fail
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await startGame.Execute(game.Code, user.Id));
        Assert.Contains("At least", exception.Message);
    }

    [Fact]
    public async Task CategorySubmission_InsufficientClues_CannotSubmit()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();
        var user = await registerUser.Execute("Creator");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();

        // Try to create a category with insufficient clues (following real workflow)
        player.Category = new Category
        {
            Player = player,
            PlayerId = player.Id,
            Title = "Test Category",
            Clues = [],
        };
        foreach (var pointValue in new[] { 100, 200, 300, 400 })
            player.Category.Clues.Add(new Clue
            {
                Question = $"Question for {pointValue}",
                Answer = $"Answer for {pointValue}",
                PointValue = pointValue,
                CategoryId = player.Category.Id,
                Category = player.Category,
            });

        // Try to submit, should fail
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await saveCategory.Execute(player.Category, true));
        Assert.Contains("must have exactly 5 clues", exception.Message);
    }

    [Fact]
    public async Task CategorySubmission_InvalidPointValues_CannotSubmit()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();

        // Set up a game with only one player. Fill invalid point values in the category
        var user = await registerUser.Execute("Creator");
        var game = await createGame.Execute(user.Id);
        var player = game.Players.Single();
        player.Category = new Category
        {
            Player = player,
            PlayerId = player.Id,
            Title = "Test Category",
            Clues = [],
        };
        foreach (var pointValue in new[] { 100, 250, 300, 400, 500 })
            player.Category.Clues.Add(new Clue
            {
                Question = $"Question for {pointValue}",
                Answer = $"Answer for {pointValue}",
                PointValue = pointValue,
                CategoryId = player.Category.Id,
                Category = player.Category,
            });

        // Try to submit, should fail with ArgumentException since validation happens before database access
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await saveCategory.Execute(player.Category, true));
        Assert.Contains("Clue point values must be 100, 200, 300", exception.Message);
    }
}