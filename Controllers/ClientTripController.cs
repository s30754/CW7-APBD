using APBD_CW7.Exceptions;
using APBD_CW7.Models.DTOs;
using APBD_CW7.Services;
using Microsoft.AspNetCore.Mvc;



namespace APBD_CW7.Controllers;

[ApiController]
[Route("api")]
public class TravelAgencyController(IDbService dbService) : ControllerBase
{

    
    [HttpGet("trips")]
    public async Task<IActionResult> GetAllTrips()
    {
        return Ok(await dbService.GetTripsAsync());
    }

    [HttpGet("clients/{id}/trips")]
    public async Task<IActionResult> GetTripsByClientId(
        [FromRoute] int id
    )
    {
        try
        {
            return Ok(await dbService.GetTripsByClientIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient(
        [FromBody] ClientCreateDTO body
    )
    {
        var client = await dbService.CreateClientAsync(body);
        return Created($"clients/{client.IdClient}", client);
    }
    

    [HttpPut("clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientTrip(
        [FromRoute] int id,
        [FromRoute] int tripId
    )
    {
        try
        {
            await dbService.RegisterClientTripAsync(id, tripId);
            return Ok();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpDelete("clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> DeleteClientTrip(
        [FromRoute] int id,
        [FromRoute] int tripId
    )
    {
        try
        {
            await dbService.DeleteClientTripAsync(id, tripId);
            return Ok();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
}