using AutoMapper;
using Dapper;
using Npgsql;
using System.Data;
using System.Numerics;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSingleton<IDbConnection>((sp) =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
#region implementation Of Minimal Api


#region get by id


app.MapGet("/get-by-id-club/{id}", async (int id, IDbConnection db, IMapper mapper) =>
{
    string query = @"
        SELECT 
            C.club_id, C.club_name, C.established_year, C.country, 
            P.player_id, P.player_name, P.position, P.birth_date, P.club_id
        FROM 
            clubs AS C
        LEFT JOIN 
            players AS P
        ON 
            C.club_id = P.club_id
        WHERE 
            C.club_id = @id";

    var clubDic = new Dictionary<int, club>();

    var result = await db.QueryAsync<club, Player, club>(query, (c, p) =>
    {
        if (!clubDic.TryGetValue(c.club_id, out var currentClub))
        {
            currentClub = c;
            clubDic.Add(currentClub.club_id, currentClub);
        }

        if (p != null)
        {
            currentClub.players.Add(p);
        }

        return currentClub;
    }, new { id }, splitOn: "player_id");

    var club = clubDic.Values.FirstOrDefault();

    if (club == null)
    {
        return Results.NotFound();
    }
    var myReturnType = mapper.Map<clubvm>(club);
    return Results.Ok(myReturnType);
});


app.MapGet("/get-by-id-player/{id}", async (int id, IDbConnection db) =>
{
    string query = @"SELECT P.player_name, P.position, P.birth_date, C.club_name
                     FROM players AS P
                     INNER JOIN clubs AS C ON P.club_id = C.club_id
                     WHERE P.player_id = @id";

    var player = await db.QuerySingleOrDefaultAsync<Playervm>(query, new { id });

    if (player == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(player);
});

#endregion


#region getall


app.MapGet("/get-clubs-with-their-players-name", async (IDbConnection db, IMapper mapper) =>
{
    string query = @"SELECT C.*, P.* 
                     FROM players AS P 
                     INNER JOIN clubs AS C 
                     ON P.club_id = C.club_id";

    var clubDic = new Dictionary<int, club>();

    var clubs = await db.QueryAsync<club, Player, club>(query, (c, p) =>
    {
        if (!clubDic.TryGetValue(c.club_id, out var currentClub))
        {
            currentClub = c;
            clubDic.Add(currentClub.club_id, currentClub);
        }

        currentClub.players.Add(p);
        return currentClub;
    }, splitOn: "player_id");
    List<club> allClubsWithTheirPlayers = clubs.Distinct().ToList();
    var myreturndata = mapper.Map<List<clubvm>>(allClubsWithTheirPlayers);
    return Results.Ok(myreturndata);
});


#endregion


#region post


app.MapPost("/add-club", async (clubPost newClubPost, IDbConnection db) =>
{
    string insertQuery = @"INSERT INTO clubs (club_name, established_year, country)
                           VALUES (@club_name, @established_year, @country)
                           RETURNING club_id";

    var clubId = await db.ExecuteScalarAsync<int>(insertQuery, new
    {
        newClubPost.club_name,
        newClubPost.established_year,
        newClubPost.country
    });
    return Results.Ok(clubId);
});


app.MapPost("/add-player", async (PlayerPost newPlayerPost, IDbConnection db) =>
{
    string insertQuery = @"INSERT INTO players (player_name, position, birth_date, club_id)
                           VALUES (@player_name, @position, @birth_date, @club_id)
                           RETURNING player_id";

    var playerId = await db.ExecuteScalarAsync<int>(insertQuery, new
    {
        newPlayerPost.player_name,
        newPlayerPost.position,
        newPlayerPost.birth_date,
        newPlayerPost.club_id
    });
    return Results.Ok(playerId);
});


#endregion


#region put


app.MapPut("/update-club/{id}", async (int id, clubPost updatedClubPost, IDbConnection db) =>
{
    string updateQuery = @"UPDATE clubs 
                           SET club_name = @club_name, established_year = @established_year, country = @country
                           WHERE club_id = @id";

    var affectedRows = await db.ExecuteAsync(updateQuery, new
    {
        club_name = updatedClubPost.club_name,
        established_year = updatedClubPost.established_year,
        country = updatedClubPost.country,
        id
    });

    if (affectedRows == 0)
    {
        return Results.NotFound();
    }

    return Results.NoContent();
});


app.MapPut("/edit-player/{id}", async (int id, PlayerPost updatedPlayerPost, IDbConnection db) =>
{
    string updateQuery = @"UPDATE players 
                           SET player_name = @player_name, position = @position, birth_date = @birth_date, club_id = @club_id
                           WHERE player_id = @id";

    var affectedRows = await db.ExecuteAsync(updateQuery, new
    {
        player_name = updatedPlayerPost.player_name,
        position = updatedPlayerPost.position,
        birth_date = updatedPlayerPost.birth_date,
        club_id = updatedPlayerPost.club_id,
        id
    });

    if (affectedRows == 0)
    {
        return Results.NotFound();
    }

    return Results.NoContent();
});


#endregion


#region delete


app.MapDelete("/delete-by-id-club-with-cascade/{id}", async (int id, IDbConnection db) =>
{
    var deleteQuery = @"DELETE FROM clubs WHERE club_id = @id";
    var affectedRows = await db.ExecuteAsync(deleteQuery, new { id });

    if (affectedRows == 0)
    {
        return Results.NotFound();
    }

    return Results.NoContent();
});


app.MapDelete("/delete-player/{id}", async (int id, IDbConnection db) =>
{
    string deleteQuery = @"DELETE FROM players WHERE player_id = @id";

    var affectedRows = await db.ExecuteAsync(deleteQuery, new { id });

    if (affectedRows == 0)
    {
        return Results.NotFound();
    }
    return Results.NoContent();
});


#endregion


#endregion


app.Run();
#region models
public class club
{
    public club()
    {
        players = new List<Player>();
    }
    public int club_id { get; set; }
    public string club_name { get; set; }
    public int established_year { get; set; }
    public string country { get; set; }
    public List<Player> players { get; set; }
}
public class Player
{
    public int player_id { get; set; }
    public string player_name { get; set; }
    public string position { get; set; }
    public DateTime birth_date { get; set; }
    public int club_id { get; set; }
    public club player_club { get; set; }
}
#endregion
#region vm
public class clubvm
{
    public string club_name { get; set; }
    public int established_year { get; set; }
    public string country { get; set; }
    public List<string> playersname { get; set; }
}


public class Playervm
{
    public string player_name { get; set; }
    public string position { get; set; }
    public DateTime birth_date { get; set; }
    public string club_name { get; set; }
}

#endregion

#region post and put models
public class clubPost
{
    public string club_name { get; set; }
    public int established_year { get; set; }
    public string country { get; set; }
}
public class PlayerPost
{
    public string player_name { get; set; }
    public string position { get; set; }
    public DateTime birth_date { get; set; }
    public int club_id { get; set; }
}
#endregion


#region mapping profile
public class MappingProfile:Profile
{
    public MappingProfile()
    {
        CreateMap<club, clubvm>()
            .ForMember(dest => dest.playersname, d => d.MapFrom(src => src.players.Select(p => p.player_name).ToList()));
    }
}
#endregion