namespace XmlFix.Sample;

// This file demonstrates the XML documentation analyzer in action
// Many of these members are missing XML documentation and should trigger XDOC001 diagnostics

public class UserService
{
    public void CreateUser(string userName, string email)
    {
        // Implementation here
    }

    public bool IsValidEmail(string email)
    {
        return email.Contains("@");
    }

    public async Task<User> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        return new User { Id = userId, Name = "Test" };
    }

    public string UserName { get; set; } = string.Empty;

    public event Action<User> UserCreated;

    public const string DefaultUserRole = "User";
}

public interface IRepository<T>
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public enum UserStatus
{
    Active,
    Inactive,
    Suspended,
    Deleted
}

public delegate void UserEventHandler(User user, UserStatus status);

// Example of inheritance scenarios that should use inheritdoc
public abstract class BaseRepository<T>
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    public abstract Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Saves changes to the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task SaveChangesAsync()
    {
        await Task.CompletedTask;
    }
}

public class UserRepository : BaseRepository<User>, IRepository<User>
{
    // These override/interface implementations should use inheritdoc
    public override async Task<User?> GetByIdAsync(int id)
    {
        await Task.Delay(50);
        return new User { Id = id };
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        await Task.Delay(100);
        return new List<User>();
    }

    public async Task CreateAsync(User entity)
    {
        await Task.Delay(50);
    }

    public async Task UpdateAsync(User entity)
    {
        await Task.Delay(50);
    }

    public async Task DeleteAsync(int id)
    {
        await Task.Delay(50);
    }

    // This method is new and should get full documentation
    public async Task<int> GetUserCountByStatusAsync(UserStatus status)
    {
        await Task.Delay(25);
        return 0;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var userService = new UserService();
        userService.CreateUser("john.doe", "john@example.com");

        var user = await userService.GetUserByIdAsync(1, CancellationToken.None);
        Console.WriteLine($"User: {user.Name}");
    }
}