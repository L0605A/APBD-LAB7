using APBD7.Models.DTO_s;
namespace APBD7.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;



[ApiController]
[Route("api/warehouse")]

public class WarehouseController : ControllerBase
{

    private readonly IConfiguration _configuration;

    public WarehouseController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
       [HttpPost]
        public IActionResult AddProduct(DTOWarehouseQuerry warehouseQuerry)
        {
            string connectionString = _configuration.GetConnectionString("Default");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    //Step 1: Check if the product with the given Id exists
                    int productId;
                    using (SqlCommand productCommand = new SqlCommand(
                        "SELECT IdProduct FROM Product WHERE IdProduct = @IdProduct",
                        connection,
                        transaction))
                    {
                        productCommand.Parameters.AddWithValue("@IdProduct", warehouseQuerry.IdProduct);

                        object productResult = productCommand.ExecuteScalar();
                        if (productResult == null || !(productResult is int))
                        {
                            transaction.Rollback();
                            return NotFound("Product not found.");
                        }

                        productId = (int)productResult;
                    }

                    Console.WriteLine("Completed step 1");
                    //Step 2: Check if the warehouse with the given Id exists
                    int warehouseId;
                    using (SqlCommand warehouseCommand = new SqlCommand(
                        "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse",
                        connection,
                        transaction))
                    {
                        warehouseCommand.Parameters.AddWithValue("@IdWarehouse", warehouseQuerry.IdWarehouse);

                        object warehouseResult = warehouseCommand.ExecuteScalar();
                        if (warehouseResult == null || !(warehouseResult is int))
                        {
                            transaction.Rollback();
                            return NotFound("Warehouse not found.");
                        }

                        warehouseId = (int)warehouseResult;
                    }

                    Console.WriteLine("Completed step 2");
                    //Step 3: Check if the amount value is greater than 0
                    if (warehouseQuerry.Amount <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest("Amount should be greater than 0");
                    }

                    Console.WriteLine("Completed step 3");
                    //Step 4: Check if order exists
                    int orderId = 0;
                    using (SqlCommand orderCommand = new SqlCommand(
                        "SELECT IdOrder " +
                        "FROM \"Order\" " +
                        "WHERE IdProduct = @IdProduct " +
                        "AND Amount = @Amount " +
                        "AND FulfilledAt IS NULL " +
                        "AND CreatedAt < CONVERT(datetime, @CreatedAt, 127) " +
                        "ORDER BY CreatedAt DESC",
                        connection,
                        transaction))
                    {
                        orderCommand.Parameters.AddWithValue("@IdProduct", warehouseQuerry.IdProduct);
                        orderCommand.Parameters.AddWithValue("@Amount", warehouseQuerry.Amount);
                        orderCommand.Parameters.AddWithValue("@CreatedAt", warehouseQuerry.CreatedAt);

                        orderId = (int?)orderCommand.ExecuteScalar() ?? 0;

                        if (orderId == 0)
                        {
                            transaction.Rollback();
                            return NotFound("Matching order not found");
                        }
                    }

                    Console.WriteLine("Completed step 4");
                    // Step 5: Get the price
                    decimal price = 0;
                    using (SqlCommand priceCommand = new SqlCommand(
                        "SELECT Price " +
                        "FROM Product " +
                        "WHERE IdProduct = @IdProduct",
                        connection,
                        transaction))
                    {
                        priceCommand.Parameters.AddWithValue("@IdProduct", warehouseQuerry.IdProduct);

                        price = (decimal?)priceCommand.ExecuteScalar() ?? 0;

                        if (price == 0)
                        {
                            transaction.Rollback();
                            return NotFound("Matching product not found or invalid price.");
                        }
                    }

                    Console.WriteLine("Completed step 5");
                    //Step 6: Update the order to mark it as fulfilled
                    using (SqlCommand updateOrderCommand = new SqlCommand(
                        "UPDATE \"Order\" SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder",
                        connection,
                        transaction))
                    {
                        updateOrderCommand.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
                        updateOrderCommand.Parameters.AddWithValue("@IdOrder", orderId);

                        updateOrderCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine("Completed step 6");
                    //Step 7: Insert record into Product_Warehouse
                    using (SqlCommand insertCommand = new SqlCommand(
                        "INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) " +
                        "VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);" +
                        "SELECT SCOPE_IDENTITY();",
                        connection,
                        transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@IdWarehouse", warehouseQuerry.IdWarehouse);
                        insertCommand.Parameters.AddWithValue("@IdProduct", warehouseQuerry.IdProduct);
                        insertCommand.Parameters.AddWithValue("@IdOrder", orderId);
                        insertCommand.Parameters.AddWithValue("@Amount", warehouseQuerry.Amount);
                        insertCommand.Parameters.AddWithValue("@Price", price * warehouseQuerry.Amount);
                        insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        int insertedId = Convert.ToInt32(insertCommand.ExecuteScalar());

                        transaction.Commit();

                        
                        string responseMessage = $"Primary key for Product_Warehouse: {insertedId}";
                        Console.WriteLine("Completed step 6");
                        return Created($"api/warehouse/{insertedId}", responseMessage);
                        
                        
                    }

                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, $"Internal Server Error: {ex.Message}");
                }
            }
        }
        
}