using System.ComponentModel.DataAnnotations;

namespace APBD_CW7.Models.DTOs;

public class ClientCreateDTO
{
    [Length(1, 120)]
    public required string FirstName { get; set; }
    [Length(1, 120)]
    public required string LastName { get; set; }
    [Length(1, 120)]
    [EmailAddress(ErrorMessage = "Invalid email")]
    public required string Email { get; set; }
    [Length(1, 120)]
    [Phone(ErrorMessage = "Invalid phone number")]
    public required string Telephone { get; set; }
    [Length(1, 120)]
    public required string Pesel { get; set; }
}