using APBD_CW7.Exceptions;
using Microsoft.Data.SqlClient;

using APBD_CW7.Models;
using APBD_CW7.Models.DTOs;

namespace APBD_CW7.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    public Task<IEnumerable<ClientTripGetDTO>> GetTripsByClientIdAsync(int id);
    public Task<Client> CreateClientAsync(ClientCreateDTO client);
    public Task RegisterClientTripAsync(int id, int tripId);
    public Task DeleteClientTripAsync(int id, int tripId);
}
public class DbService(IConfiguration config) : IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");

    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();

        await using var connection = new SqlConnection(_connectionString);
        
        const string getAllTripsInfoSql = @"select t.IdTrip,
                            t.Name,
                            t.Description,
                            t.DateFrom,
                            t.DateTo,
                            t.MaxPeople,
                            c.Name
                            from Trip t
                            JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                            JOIN Country c ON ct.IdCountry = c.IdCountry";
        await using var command = new SqlCommand(getAllTripsInfoSql, connection);
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                CountryName = reader.GetString(6)
            });
        }

        return result;
    }

    public async Task<IEnumerable<ClientTripGetDTO>> GetTripsByClientIdAsync(int id)
    {
        var result = new List<ClientTripGetDTO>();

        await using var connection = new SqlConnection(_connectionString);

        const string getTripInfoSql = @"select t.IdTrip,
                            t.Name,
                            t.Description,
                            t.DateFrom,
                            t.DateTo,
                            t.MaxPeople,
                            c.Name,
                            clt.RegisteredAt, 
                            clt.PaymentDate
                            from Trip t
                            JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                            JOIN Country c ON ct.IdCountry = c.IdCountry
                            JOIN Client_Trip clt ON t.IdTrip = clt.IdTrip
                            WHERE clt.IdClient = @id";

        await using var command = new SqlCommand(getTripInfoSql, connection);

        command.Parameters.AddWithValue("@id", id);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            throw new NotFoundException($"Client with id: {id} doesn't exist");
        }

        while (await reader.ReadAsync())
        {
            result.Add(new ClientTripGetDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                CountryName = reader.GetString(6),
                RegisteredAt = reader.GetInt32(7),
                PaymentDate = reader.IsDBNull(8) ? int.Parse(DateTime.Now.ToString("yyyyMMdd")) : reader.GetInt32(8)
            });
        }

        return result;
    }

    public async Task<Client> CreateClientAsync(ClientCreateDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        
        const string insertClientAndGetIdSql = @"insert into Client (FirstName, LastName, Email, Telephone, Pesel)
            values (@FirstName, @LastName, @Email, @Telephone, @Pesel); Select scope_identity()";
        
        await using var command = new SqlCommand(insertClientAndGetIdSql, connection);
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        await connection.OpenAsync();
        var id = await command.ExecuteScalarAsync();

        return new Client()
        {
            IdClient = Convert.ToInt32(id),
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }

    public async Task RegisterClientTripAsync(int id, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string clientExistsSql = "SELECT COUNT(1) FROM Client WHERE IdClient = @id";
        
        await using (var checkClientCommand = new SqlCommand(clientExistsSql, connection))
        {
            checkClientCommand.Parameters.AddWithValue("@id", id);
            var clientExists = (int)await checkClientCommand.ExecuteScalarAsync() > 0;
            if (!clientExists)
            {
                throw new NotFoundException($"Client with id: {id} doesn't  exist");
            }
        }
        const string tripExistsSql = "SELECT COUNT(1) FROM Trip WHERE IdTrip = @tripId";
        
        await using (var checkTripCommand = new SqlCommand(tripExistsSql, connection))
        {
            checkTripCommand.Parameters.AddWithValue("@tripId", tripId);
            var tripExists = (int)await checkTripCommand.ExecuteScalarAsync() > 0;
            if (!tripExists)
            {
                throw new NotFoundException($"Trip with id: {tripId} doesn't  exist");
            }
        }
        const string isClientRegisteredForTripSql = "SELECT COUNT(1) FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId";
        
        await using (var checkClientTripCommand = new SqlCommand(isClientRegisteredForTripSql, connection))
        {
            checkClientTripCommand.Parameters.AddWithValue("@id", id);
            checkClientTripCommand.Parameters.AddWithValue("@tripId", tripId);
            var clientTripExists = (int)await checkClientTripCommand.ExecuteScalarAsync() > 0;
            if (clientTripExists)
            {
                throw new NotFoundException($"Client with id: {id} is already registered for trip with id: {tripId}");
            }
        }
        const string getMaxPeopleSql = "SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId";
        
        const string getNumOfRegisteredPeopleSql = "SELECT COUNT(1) FROM Client_Trip WHERE IdTrip = @tripId";
        
        int maxPeople;
        int registeredPeople;

        
        await using (var checkMaxPeopleCommand = new SqlCommand(getMaxPeopleSql, connection))
        {
            checkMaxPeopleCommand.Parameters.AddWithValue("@tripId", tripId);
            maxPeople = (int)await checkMaxPeopleCommand.ExecuteScalarAsync();
        }

        await using (var checkRegisteredPeopleCommand = new SqlCommand(getNumOfRegisteredPeopleSql, connection))
        {
            checkRegisteredPeopleCommand.Parameters.AddWithValue("@tripId", tripId);
            registeredPeople = (int)await checkRegisteredPeopleCommand.ExecuteScalarAsync();
        }

        
        
        if (registeredPeople >= maxPeople)
        {
            throw new NotFoundException($"No more places on trip with id: {tripId}");
        }
        
        
        
        const string registerClientSql = @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                             VALUES (@id, @tripId, @RegisteredAt)";
        await using var command = new SqlCommand(registerClientSql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@tripId", tripId);
        command.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteClientTripAsync(int id, int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        
    
        const string deleteClientFromTripSql = "DELETE FROM Client_Trip WHERE IdClient = @id AND IdTrip = @tripId";
        await using var command = new SqlCommand(deleteClientFromTripSql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@tripId", tripId);
        await connection.OpenAsync();
        var numOfRows = await command.ExecuteNonQueryAsync();

        if (numOfRows == 0)
        {
            throw new NotFoundException($"Client with id: {id} is not registered for trip with id: {tripId}");
        }
        
    }
    
    
}