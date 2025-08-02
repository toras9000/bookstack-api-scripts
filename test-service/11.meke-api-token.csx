#r "nuget: MySqlConnector, 2.4.0"
#r "nuget: Dapper, 2.1.66"
#r "nuget: BCrypt.Net-Next, 4.0.3"
#r "nuget: Lestaly.General, 0.102.0"
#load ".settings.csx"
#nullable enable
using Dapper;
using Lestaly;
using Lestaly.Cx;
using MySqlConnector;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    WriteLine("Setup api token ...");
    var config = new MySqlConnectionStringBuilder();
    config.Server = settings.Database.Host;
    config.Port = settings.Database.Port;
    config.UserID = settings.Database.Username;
    config.Password = settings.Database.Password;
    config.Database = settings.Database.Database;

    using var mysql = new MySqlConnection(config.ConnectionString);
    await mysql.OpenAsync();

    var tokenName = settings.BookStack.Api.TokenName;
    var tokenExists = await mysql.QueryFirstAsync<long>("select count(*) from api_tokens where name = @name", param: new { name = tokenName, });
    if (0 < tokenExists)
    {
        WriteLine(".. Already exists");
        return;
    }

    var tokenId = settings.BookStack.Api.TokenId;
    var tokenSecret = settings.BookStack.Api.TokenSecret;

    var adminId = await mysql.QueryFirstAsync<long>(sql: "select id from users where name = 'Admin'");
    var hashSalt = BCrypt.Net.BCrypt.GenerateSalt(12, 'y');
    var secretHash = BCrypt.Net.BCrypt.HashPassword(tokenSecret, hashSalt);
    var tokenParam = new
    {
        name = tokenName,
        token_id = tokenId,
        secret = secretHash,
        user_id = adminId,
        expires_at = DateTime.Now.AddYears(100),
    };
    await mysql.ExecuteAsync(
        sql: "insert into api_tokens (name, token_id, secret, user_id, expires_at) values (@name, @token_id, @secret, @user_id, @expires_at)",
        param: tokenParam
    );
    WriteLine(".. Token added");
});
