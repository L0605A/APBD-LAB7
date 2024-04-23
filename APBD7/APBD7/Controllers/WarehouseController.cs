using APBD7.Models.DTO_s;
namespace APBD7.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;



[ApiController]
[Route("api/animals")]

public class AnimalsController : ControllerBase
{

    private readonly IConfiguration _configuration;

    public AnimalsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    

    [HttpPost]
    public IActionResult AddAnimal(DTOWarehouseQuerry warehouseQuerry)
    {
        using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        connection.Open();

        using SqlCommand command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "SELECT IdProduct FROM PRODUCT WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", warehouseQuerry.IdProduct);
        
        var reader = command.ExecuteReader();
        
        while (reader.Read())
        {
            int id = reader.GetInt32(reader.GetOrdinal("IdProduct"));
        }
        
        return Created();
    }
}