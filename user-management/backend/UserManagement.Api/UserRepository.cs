using System.Data;
using Dapper;

namespace UserManagement.Api;

public sealed class UserRepository
{
    private readonly IDbConnection _connection;
    private readonly PasswordHasher _hasher;

    public UserRepository(IDbConnection connection, PasswordHasher hasher)
    {
        _connection = connection;
        _hasher = hasher;
    }

    public Task<UserEntity?> GetByUserNameAsync(string userName)
    {
        const string sql = "SELECT Id, UserName, PasswordHash, PasswordSalt, Role, CreatedAt FROM dbo.Users WHERE UserName = @userName";
        return _connection.QuerySingleOrDefaultAsync<UserEntity>(sql, new { userName });
    }

    public Task<IEnumerable<UserEntity>> GetAllAsync()
    {
        const string sql = "SELECT Id, UserName, PasswordHash, PasswordSalt, Role, CreatedAt FROM dbo.Users ORDER BY CreatedAt DESC";
        return _connection.QueryAsync<UserEntity>(sql);
    }

    public async Task<UserEntity> CreateSsoUserAsync(string email)
    {
        var randomPassword = Guid.NewGuid().ToString("N");
        var hash = _hasher.Hash(randomPassword);
        const string sql = @"
INSERT INTO dbo.Users(UserName, PasswordHash, PasswordSalt, Role)
OUTPUT INSERTED.Id, INSERTED.UserName, INSERTED.PasswordHash, INSERTED.PasswordSalt, INSERTED.Role, INSERTED.CreatedAt
VALUES(@userName, @passwordHash, @passwordSalt, 'User')";

        return await _connection.QuerySingleAsync<UserEntity>(sql, new
        {
            userName = email,
            passwordHash = hash.Hash,
            passwordSalt = hash.Salt
        });
    }

    public Task ChangePasswordAsync(int userId, string hash, string salt)
    {
        const string sql = "UPDATE dbo.Users SET PasswordHash=@hash, PasswordSalt=@salt WHERE Id=@userId";
        return _connection.ExecuteAsync(sql, new { userId, hash, salt });
    }
}
