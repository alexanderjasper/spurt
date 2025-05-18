using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Categories.Commands;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Tests.Integration;

public class CategorySubmissionWorkflowTests
{
    // Real implementations
    private readonly SaveCategory _saveCategory;
    private readonly StartGame _startGame;

    // Mocked data layer
    private readonly IAddCategory _addCategory;
    private readonly IUpdateCategory _updateCategory;
    private readonly IGetGame _getGame;
    private readonly IUpdateGame _updateGame;
    private readonly IGameHubNotificationService _gameHubNotificationService;

    // Test data
    private readonly Guid _creatorId = Guid.NewGuid();
    private readonly Guid _player2Id = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Game _game;
    private readonly string _gameCode = "GAME456";
    private readonly Player _creator;
    private readonly Player _player2;

    public CategorySubmissionWorkflowTests()
    {
        // Initialize test data - Create a game with 2 players but no categories yet
        _game = new Game
        {
            Id = Guid.NewGuid(),
            Code = _gameCode,
            State = GameState.WaitingForCategories,
            Players = []
        };

        _creator = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _creatorId,
            User = new User { Id = _creatorId, Name = "Creator" },
            GameId = _game.Id,
            Game = _game,
            IsCreator = true
        };

        _player2 = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _player2Id,
            User = new User { Id = _player2Id, Name = "Player 2" },
            GameId = _game.Id,
            Game = _game,
            IsCreator = false
        };

        _game.Players.Add(_creator);
        _game.Players.Add(_player2);

        // Configure mocks
        _addCategory = Substitute.For<IAddCategory>();
        _updateCategory = Substitute.For<IUpdateCategory>();
        _getGame = Substitute.For<IGetGame>();
        _updateGame = Substitute.For<IUpdateGame>();
        _gameHubNotificationService = Substitute.For<IGameHubNotificationService>();

        _getGame.Execute(_gameCode).Returns(_game);
        _getGame.Execute(_gameCode, Arg.Any<bool>()).Returns(_game);

        // Setup AddCategory to return the created category with the given ID
        _addCategory.Execute(Arg.Any<Category>()).Returns(callInfo =>
        {
            var category = callInfo.Arg<Category>();
            category.Id = _categoryId;
            
            // Setup CategoryId for all clues
            foreach (var clue in category.Clues)
            {
                clue.CategoryId = category.Id;
                clue.Category = category;
            }
            
            return category;
        });

        // Setup UpdateCategory to return the updated category
        _updateCategory.Execute(Arg.Any<Category>()).Returns(callInfo => callInfo.Arg<Category>());

        // Setup UpdateGame to return the updated game
        _updateGame.Execute(Arg.Any<Game>()).Returns(callInfo => callInfo.Arg<Game>());

        // Create real implementations
        _saveCategory = new SaveCategory(_addCategory, _updateCategory, _getGame, _gameHubNotificationService);
        _startGame = new StartGame(_getGame, _updateGame, _gameHubNotificationService);
    }

    [Fact]
    public async Task CategorySubmission_ThenStartGame_Success()
    {
        // Step 1: Create a category for creator
        var creatorCategoryId = Guid.NewGuid();
        var creatorCategory = new Category
        {
            Id = creatorCategoryId,
            Title = "Creator Category",
            PlayerId = _creator.Id,
            Player = _creator,
            Clues = []
        };
        
        // Add clues with proper references
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 2",
            Question = "Question 2", 
            PointValue = 200,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 3",
            Question = "Question 3",
            PointValue = 300,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 4",
            Question = "Question 4",
            PointValue = 400,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 5",
            Question = "Question 5",
            PointValue = 500,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });

        // First save as draft
        var savedCategory = await _saveCategory.Execute(creatorCategory, false);
        
        // Update the game object to include the category
        _creator.Category = savedCategory;
        
        // Then submit the category
        var submittedCategory = await _saveCategory.Execute(savedCategory, true);
        Assert.True(submittedCategory.IsSubmitted);
        
        // Update the creator's category in our game state
        _creator.Category = submittedCategory;
        
        // Step 3: Create and submit a category for player 2
        var player2CategoryId = Guid.NewGuid();
        var player2Category = new Category
        {
            Id = player2CategoryId,
            Title = "Player 2 Category",
            PlayerId = _player2.Id,
            Player = _player2,
            Clues = []
        };
        
        // Add clues with proper references
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 2",
            Question = "Question 2", 
            PointValue = 200,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 3",
            Question = "Question 3",
            PointValue = 300,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 4",
            Question = "Question 4",
            PointValue = 400,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 5",
            Question = "Question 5",
            PointValue = 500,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        // Submit player 2's category directly
        var submittedPlayer2Category = await _saveCategory.Execute(player2Category, true);
        _player2.Category = submittedPlayer2Category;
        
        // Step 4: Try to start the game
        var startedGame = await _startGame.Execute(_gameCode, _creatorId);
        
        // Verify the game started correctly
        Assert.Equal(GameState.InProgress, startedGame.State);
        Assert.Equal(_creator.Id, startedGame.CurrentChoosingPlayerId);
        
        // Verify interactions with dependencies
        await _addCategory.Received(2).Execute(Arg.Any<Category>());
        await _updateCategory.Received(1).Execute(Arg.Any<Category>());
        await _updateGame.Received(1).Execute(Arg.Any<Game>());
        await _gameHubNotificationService.Received(3).NotifyGameUpdated(Arg.Any<Game>());
    }
    
    [Fact]
    public async Task CategorySubmission_NotEnoughPlayers_CannotStartGame()
    {
        // Create a game with only one player
        var singlePlayerGame = new Game
        {
            Id = Guid.NewGuid(),
            Code = "SINGLE",
            State = GameState.WaitingForCategories,
            Players = [_creator]
        };
        
        // Set up the category for the creator
        var creatorCategoryId = Guid.NewGuid();
        var creatorCategory = new Category
        {
            Id = creatorCategoryId,
            Title = "Creator Category",
            PlayerId = _creator.Id,
            Player = _creator,
            IsSubmitted = true,
            Clues = []
        };
        
        // Add clues with proper references
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 2",
            Question = "Question 2", 
            PointValue = 200,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 3",
            Question = "Question 3",
            PointValue = 300,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 4",
            Question = "Question 4",
            PointValue = 400,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 5",
            Question = "Question 5",
            PointValue = 500,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        _creator.Category = creatorCategory;
        
        _getGame.Execute("SINGLE").Returns(singlePlayerGame);
        
        // Try to start the game - should throw because there's only one player
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _startGame.Execute("SINGLE", _creatorId));
    }
    
    [Fact]
    public async Task CategorySubmission_NotAllCategoriesSubmitted_CannotStartGame()
    {
        // Set up the category for the creator but not submitted
        var creatorCategoryId = Guid.NewGuid();
        var creatorCategory = new Category
        {
            Id = creatorCategoryId,
            Title = "Creator Category",
            PlayerId = _creator.Id,
            Player = _creator,
            IsSubmitted = false, // Not submitted
            Clues = []
        };
        
        // Add clues with proper references (but not enough of them)
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        creatorCategory.Clues.Add(new Clue
        {
            Answer = "Answer 2",
            Question = "Question 2", 
            PointValue = 200,
            CategoryId = creatorCategoryId,
            Category = creatorCategory
        });
        
        _creator.Category = creatorCategory;
        
        // Player 2 has a submitted category
        var player2CategoryId = Guid.NewGuid();
        var player2Category = new Category
        {
            Id = player2CategoryId,
            Title = "Player 2 Category",
            PlayerId = _player2.Id,
            Player = _player2,
            IsSubmitted = true,
            Clues = []
        };
        
        // Add clues with proper references
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 2",
            Question = "Question 2", 
            PointValue = 200,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 3",
            Question = "Question 3",
            PointValue = 300,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 4",
            Question = "Question 4",
            PointValue = 400,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        player2Category.Clues.Add(new Clue
        {
            Answer = "Answer 5",
            Question = "Question 5",
            PointValue = 500,
            CategoryId = player2CategoryId,
            Category = player2Category
        });
        
        _player2.Category = player2Category;
        
        // Try to start the game - should throw because not all categories are submitted
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _startGame.Execute(_gameCode, _creatorId));
    }
    
    [Fact]
    public async Task CategorySubmission_InsufficientClues_CannotSubmit()
    {
        // Create a category with insufficient clues
        var categoryId = Guid.NewGuid();
        var invalidCategory = new Category
        {
            Id = categoryId,
            Title = "Invalid Category",
            PlayerId = _creator.Id,
            Player = _creator,
            Clues = []
        };
        
        // Add only 2 clues (not enough)
        invalidCategory.Clues.Add(new Clue
        {
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            CategoryId = categoryId,
            Category = invalidCategory
        });
        
        invalidCategory.Clues.Add(new Clue
        {
            Answer = "Answer 2",
            Question = "Question 2", 
            PointValue = 200,
            CategoryId = categoryId,
            Category = invalidCategory
        });
        
        // Trying to submit should throw an exception
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _saveCategory.Execute(invalidCategory, true));
    }
    
    [Fact]
    public async Task CategorySubmission_InvalidPointValues_CannotSubmit()
    {
        // Create a category with invalid point values
        var categoryId = Guid.NewGuid();
        var invalidCategory = new Category
        {
            Id = categoryId,
            Title = "Invalid Category",
            PlayerId = _creator.Id,
            Player = _creator,
            Clues = []
        };
        
        // Add clues with one invalid value
        invalidCategory.Clues.Add(new Clue
        {
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            CategoryId = categoryId,
            Category = invalidCategory
        });
        
        invalidCategory.Clues.Add(new Clue
        {
            Answer = "Answer 2",
            Question = "Question 2", 
            PointValue = 150, // Not a multiple of 100
            CategoryId = categoryId,
            Category = invalidCategory
        });
        
        invalidCategory.Clues.Add(new Clue
        {
            Answer = "Answer 3",
            Question = "Question 3",
            PointValue = 300,
            CategoryId = categoryId,
            Category = invalidCategory
        });
        
        invalidCategory.Clues.Add(new Clue
        {
            Answer = "Answer 4",
            Question = "Question 4",
            PointValue = 400,
            CategoryId = categoryId,
            Category = invalidCategory
        });
        
        invalidCategory.Clues.Add(new Clue
        {
            Answer = "Answer 5",
            Question = "Question 5",
            PointValue = 500,
            CategoryId = categoryId,
            Category = invalidCategory
        });
        
        // Trying to save should throw an exception
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _saveCategory.Execute(invalidCategory, false));
    }
} 