using System.ComponentModel.DataAnnotations;

namespace APBD7.Models.DTO_s;

public class DTOWarehouseQuerry
{
    [Required]
    public int IdProduct { get; set; }
    [Required]
    public int IdWarehouse { get; set; }
    [Required]
    public int Amount { get; set; }
    [Required]
    public string CreatedAt { get; set; }
}