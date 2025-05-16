using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Users;
using Spurt.Domain.Users.Commands;
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace Spurt.Tests.Integration;

public class SignupAndGameCreationTests
{
    // Real implementations
    private readonly IRegisterUser _registerUser;
    private readonly ICreateGame _createGame;

    // Mocked data layer
    private readonly IAddUser _addUser;
    private readonly IAddPlayer _addPlayer;
    private readonly IAddGame _addGame;
    private readonly IGetUser _getUser;

    private readonly User _testUser;
    private readonly Guid _userId = Guid.NewGuid();

    public SignupAndGameCreationTests()
    {
        _addUser = Substitute.For<IAddUser>();
        _addPlayer = Substitute.For<IAddPlayer>();
        _addGame = Substitute.For<IAddGame>();
        _getUser = Substitute.For<IGetUser>();

        _testUser = new User
        {
            Id = _userId,
            Name = "Test User",
        };

        // Configure data layer mocks
        _getUser.Execute(_userId).Returns(_testUser);

        // Create real implementations with mocked dependencies
        _registerUser = new RegisterUser(_addUser);
        _createGame = new CreateGame(_addGame, _getUser);
    }

    [Fact]
    public async Task CompleteUserJourney_RegisterAndCreateGame_Success()
    {
        // Step 1: Register a new user
        var userName = "Test User";
        var user = await _registerUser.Execute(userName);

        // Verify user was created with the correct name
        Assert.NotNull(user);
        Assert.Equal(userName, user.Name);

        // Verify AddUser was called with the correct user
        await _addUser.Received(1).Execute(Arg.Is<User>(u => u.Name == userName));

        // Configure GetUser mock to return the newly registered user
        _getUser.Execute(user.Id).Returns(user);

        // Step 2: Create a new game with the user
        var game = await _createGame.Execute(user.Id);

        // Verify a Player was created for the User (via AddGame now, not AddPlayer)
        Assert.Single(game.Players);
        Assert.Equal(user.Id, game.Players[0].UserId);
        Assert.True(game.Players[0].IsCreator);

        // Verify AddGame was called 
        await _addGame.Received(1).Execute(Arg.Any<Game>());

        // Verify game properties
        Assert.NotNull(game);
        Assert.Equal(6, game.Code.Length); // Game code should be 6 characters
        Assert.Single(game.Players);
        Assert.True(game.Players[0].IsCreator);
        Assert.Equal(user.Id, game.Players[0].UserId);
    }
}