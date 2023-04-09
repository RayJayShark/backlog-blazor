using Dapper;
using Microsoft.AspNetCore.Components;
using MySqlConnector;

namespace BacklogBlazor_Server.Services;

public class UserDataService
{
    private MySqlConnection _sqlConnection;
    [Inject] private ILogger<AuthDataService> _logger { get; set; }
    
    public UserDataService(string connectionString)
    {
        _sqlConnection = new MySqlConnection(connectionString);

        TestConnection();
    }
    
    private void TestConnection()
    {
        try
        {
            _sqlConnection.Open();
        }
        catch (MySqlException ex)
        {
            _logger.LogError("Error connecting to database: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _sqlConnection.Close();
        }
    }

    public async Task<string> GetUserAvatarUrl(long userId)
    {
        await _sqlConnection.OpenAsync();

        var avatarUrl = await _sqlConnection.QuerySingleAsync<string>(
            "select AvatarUrl from user where UserId = @userId",
            new { userId });

        await _sqlConnection.CloseAsync();

        return avatarUrl;
    }

    public async Task UpdateUserAvatar(long userId, string avatarUrl)
    {
        await _sqlConnection.OpenAsync();
        
        await _sqlConnection.ExecuteAsync("update user set AvatarUrl = @avatarUrl where UserId = @userId",
            new { userId, avatarUrl });

        await _sqlConnection.CloseAsync();
    }

    public async Task<bool> IsDiscordUser(long userId)
    {
        await _sqlConnection.OpenAsync();

        var isDiscordUser =
            await _sqlConnection.QuerySingleAsync<bool>("select not isnull(DiscordId) from user where UserId = @userId",
                new { userId });

        await _sqlConnection.CloseAsync();

        return isDiscordUser;
    }
}