﻿using BacklogBlazor_Server.Models;
using BacklogBlazor_Server.Models.ThirdPartyAuth;
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

    public async Task<User> UpsertDiscordUser(DiscordUser discordUser)
    {
        await _sqlConnection.OpenAsync();

        await _sqlConnection.ExecuteAsync(
            @"insert into user (Email, Username, AvatarUrl, DiscordId) values
                                    (@email, @username, @avatarUrl, @id)
                    on duplicate key update
                         Username = @username,
                         AvatarUrl = @avatarUrl",
                discordUser);

        var user = await _sqlConnection.QuerySingleAsync<User>("select * from user where DiscordId = @id", discordUser);

        await _sqlConnection.CloseAsync();

        return user;
    }
    
    public async Task<bool> IsValidUser(long userId)
    {
        await _sqlConnection.OpenAsync();

        var users = await _sqlConnection.QueryAsync<long?>(
            "select UserId from user where UserId = @userId limit 1",
            new { userId });

        await _sqlConnection.CloseAsync();

        return users is not null && users.Any();
    }

    public async Task<string> GetUsername(long userId)
    {
        await _sqlConnection.OpenAsync();

        var username =
            await _sqlConnection.QueryFirstAsync<string>("select Username from User where userId = @userId limit 1",
                new { userId });

        await _sqlConnection.CloseAsync();

        return username;
    }
}