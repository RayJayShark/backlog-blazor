using BacklogBlazor_Server.Models;
using Dapper;
using MySqlConnector;

namespace BacklogBlazor_Server.Services;

public class AuthDataService
{
    private readonly MySqlConnection _sqlConnection;

    public AuthDataService(string connectionString)
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
            throw;
        }
        finally
        {
            _sqlConnection.Close();
        }
    }

    public async Task<PasswordHash> GetHashedPassword(string email)
    {
        await _sqlConnection.OpenAsync();

        var hashWithUserId = await _sqlConnection.QueryAsync<PasswordHash>(
            @"select U.UserId, U.Username, P.PasswordHash as Hash from Password P 
                    inner join User U on U.UserId = P.UserId
                where U.Email = @email",
            new { email });

        await _sqlConnection.CloseAsync();

        return hashWithUserId.FirstOrDefault();
    }

    public async Task<bool> EmailExists(string email)
    {
        await _sqlConnection.OpenAsync();

        var validLogin = await _sqlConnection.QueryAsync<bool>(@"
                                            select count(*) > 0 from User 
                                            where Email = @email", new {email});
        await _sqlConnection.CloseAsync();
        
        return validLogin.FirstOrDefault();
    }

    public async Task<long?> RegisterUser(string email, string username, string passwordHash)
    {
        await _sqlConnection.OpenAsync();

        long? userId = null;
        
        var tran = await _sqlConnection.BeginTransactionAsync();
        
        try
        {
            var userIdEnum = await _sqlConnection.QueryAsync<long>(
                @"insert into User (Email, Username) values (@email, @username);
                    select LAST_INSERT_ID();",
                new { email, username }, tran);
            userId = userIdEnum.FirstOrDefault();
            
            await _sqlConnection.ExecuteAsync(
                "insert into Password (UserId, PasswordHash) values (@userId, @passwordHash)",
                new { userId, passwordHash }, tran);

            await tran.CommitAsync();
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            await _sqlConnection.CloseAsync();
            throw;
        }

        await _sqlConnection.CloseAsync();

        return userId;
    }
}