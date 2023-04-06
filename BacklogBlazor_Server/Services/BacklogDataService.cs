using BacklogBlazor_Shared.Models;
using Dapper;
using Microsoft.AspNetCore.Components;
using MySqlConnector;

namespace BacklogBlazor_Server.Services;

public class BacklogDataService
{
    private MySqlConnection _sqlConnection;
    [Inject] private ILogger<AuthDataService> _logger { get; set; }
    
    public BacklogDataService(string connectionString)
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

    public async Task<long> GetNextBacklogId(long userId)
    {
        await _sqlConnection.OpenAsync();


        var newId = -1l;
        try
        {
            await _sqlConnection.ExecuteAsync(
                @"insert into backlog (OwnerUserId) values (@userId)", 
                new {userId});
            newId = await _sqlConnection.QuerySingleAsync<long>("select last_insert_id();");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating new backlog: {Message}", ex.Message);
            throw;
        }

        await _sqlConnection.CloseAsync();

        return newId;
    }

    public async Task<bool> UpdateBacklogData(BacklogModel backlogModel)
    {
        await _sqlConnection.OpenAsync();

        try
        {
            var rowsAffected = await _sqlConnection.ExecuteAsync(
                @"update backlog
                        set Name = @name, Description = @description, UpdatedDate = now()
                        where BacklogId = @id", backlogModel);

            if (rowsAffected == 0)
                return false;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating backlog metadata: {Message}", ex.Message);
            return false;
        }

        await _sqlConnection.CloseAsync();

        return true;
    }
    
    public async Task<bool> SaveBacklogItems(BacklogModel backlogModel)
    {
        await _sqlConnection.OpenAsync();

        var transaction = await _sqlConnection.BeginTransactionAsync();

        try
        {
            await _sqlConnection.ExecuteAsync("delete from backlog_item where BacklogId = @id", backlogModel, 
                transaction);

            var rowsAffected = 
                await _sqlConnection.ExecuteAsync(@"insert into backlog_item (BacklogId, GameId, `Rank`, ItemName, EstimateHoursToComplete, CurrentHours, Completed)
                                                        values (@backlogId, @id, @rank, @name, @EstimateCompleteHours, @currentHours, @completed)", backlogModel.Games,
                transaction);

            if (rowsAffected != backlogModel.Games.Count)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error saving backlog items: {Message}", ex.Message);
            await transaction.RollbackAsync();
            return false;
        }

        await transaction.CommitAsync();
        await _sqlConnection.CloseAsync();
        return true;
    }

    public async Task<BacklogModel> GetBacklog(long backlogId)
    {
        await _sqlConnection.OpenAsync();

        var backlog = await _sqlConnection.QueryFirstAsync<BacklogModel>(
            "select BacklogId as Id, Name, Description from backlog where BacklogId = @backlogId",
            new { backlogId });

        var gameEnum = await _sqlConnection.QueryAsync<Game>(
            @"select 
                    bi.GameId as Id, 
                    bi.`Rank`, 
                    bi.ItemName as Name, 
                    bi.EstimateHoursToComplete as EstimateCompleteHours, 
                    bi.CurrentHours,
                    bi.Completed,
                    gc.ImageSource as GameImage,
                    gc.CompleteMainSeconds,
                    gc.CompletePlusSeconds,
                    gc.Complete100Seconds,
                    gc.CompleteAllSeconds
                from backlog_item bi
                    left join game_cache gc on bi.GameId = gc.GameId                    
                where BacklogId = @backlogId
                order by `Rank`",
            new { backlogId });
        backlog.Games = gameEnum.ToList();

        await _sqlConnection.CloseAsync();

        return backlog;
    }

    public async Task<List<BacklogModel>> GetUserBacklogs(long userId)
    {
        await _sqlConnection.OpenAsync();

        var backlogs = new List<BacklogModel>();

        await _sqlConnection.QueryAsync<BacklogModel, Game, BacklogModel>(
            @"select
                    b.BacklogId as Id, 
                    b.Name, 
                    b.Description,
                    
                    null as Break,
                    
                    GameId as Id, 
                    `Rank`, 
                    ItemName as Name
                from backlog b 
                    left join backlog_item bi on bi.BacklogId = b.BacklogId
                where b.OwnerUserId = @userId
                order by b.BacklogId, bi.`Rank`",
            (backlogModel, game) =>
            {
                var index = backlogs.FindIndex(b => b.Id == backlogModel.Id);
                
                if (index < 0)
                {
                    // Avoids null game on empty backlog
                    if (game.Id >= 0)
                    {
                        backlogModel.Games.Add(game);
                    }

                    backlogs.Add(backlogModel);
                }
                else
                {
                    backlogs[index].Games.Add(game);
                }

                return backlogModel;
            },
            new { userId },
            splitOn: "Break");

        await _sqlConnection.CloseAsync();

        return backlogs;
    }

    public async Task<bool> IsOwner(long backlogId, long userId)
    {
        await _sqlConnection.OpenAsync();

        var isOwner = await _sqlConnection.QuerySingleAsync<bool>(
            "select OwnerUserId = @userId from backlog where BacklogId = @backlogId",
            new { backlogId, userId });

        await _sqlConnection.CloseAsync();

        return isOwner;
    }

    public async Task CacheGames(List<Game> games)
    {
        await _sqlConnection.OpenAsync();

        var tran = await _sqlConnection.BeginTransactionAsync();

        var rowsAffected = await _sqlConnection.ExecuteAsync(
            @"insert into game_cache (GameId, ImageSource, CompleteMainSeconds, CompletePlusSeconds, Complete100Seconds, CompleteAllSeconds) values 
                                        (@id, @gameImage, @completeMainSeconds, @completePlusSeconds, @complete100Seconds, @completeAllSeconds)
                    on duplicate key update
                                         ImageSource = @gameImage,
                                         CompleteMainSeconds = @completeMainSeconds,
                                         CompletePlusSeconds = @completePlusSeconds,
                                         Complete100Seconds = @complete100Seconds,
                                         CompleteAllSeconds = @completeAllSeconds",
            games, tran);
        
        await tran.CommitAsync();


        await _sqlConnection.CloseAsync();
    }

    public async Task<bool> DeleteBacklog(long backlogId)
    {
        await _sqlConnection.OpenAsync();

        var tran = await _sqlConnection.BeginTransactionAsync();
        try
        {
            await _sqlConnection.ExecuteAsync("delete from backlog_item where BacklogId = @backlogId",
                new { backlogId }, tran);
            
            await _sqlConnection.ExecuteAsync("delete from backlog where BacklogId = @backlogId", 
                new { backlogId }, tran);

            var gameBacklogCount = await _sqlConnection.QuerySingleAsync<int>(
                @"select count(*)  from backlog b 
                        cross join backlog_item bi 
                    where b.BacklogId = @backlogId
                        or bi.BacklogId = @backlogId", new { backlogId }, tran);

            if (gameBacklogCount > 0)
                throw new Exception("Error when deleting backlog or games");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error when deleting backlog: {Message}", ex.Message);
            await tran.RollbackAsync();
            await _sqlConnection.CloseAsync();
            return false;
        }

        await tran.CommitAsync();
        await _sqlConnection.CloseAsync();
        return true;
    }
}