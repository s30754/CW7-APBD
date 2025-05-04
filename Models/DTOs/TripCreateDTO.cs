using System.ComponentModel.DataAnnotations;
namespace APBD_CW7.Models.DTOs;

public class TripCreateDTO
{
    [Length(1, 120)]
    public required string Name { get; set; }
    [Length(1, 220)]
    public required string Description { get; set; }
    public required DateTime DateFrom { get; set; }
    public required DateTime DateTo { get; set; }
    public required int MaxPeople { get; set; }
}