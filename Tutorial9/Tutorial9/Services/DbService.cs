using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task DoSomethingAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = @"INSERT INTO Animal VALUES(@IdAnimal, @NameAnimal)";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@NameAnimal", "Name");

            await command.ExecuteNonQueryAsync();

            command.Parameters.Clear();
            command.CommandText = @"INSERT INTO Animal VALUES(@IdAnimal, @NameAnimal)";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@NameAnimal", "Name");

            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task ProcedureAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        command.CommandText = "AddProductToWarehouse";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@IdProduct", 1);
        
        await command.ExecuteScalarAsync();
    }

    public async  Task <int> newProductWarehouseAdd(ProductWarehouseAdd product)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;
        

        try
        {
            command.CommandText = "SELECT IdOrder FROM [Order] WHERE IdProduct = @IdProduct AND Amount = @Amount";
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            command.Parameters.AddWithValue("@Amount", product.Amount);
            int idOrder = (int)await command.ExecuteScalarAsync();
            command.Parameters.Clear();

            command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            double price = Convert.ToDouble(await command.ExecuteScalarAsync());
            double finalPrice = price * product.Amount;
            command.Parameters.Clear();

            command.CommandText = "UPDATE [Order] SET FulfilledAt = @date WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@date", DateTime.Now);
            await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();

            command.CommandText = @"INSERT INTO Product_Warehouse (IdOrder, IdProduct, IdWarehouse, Amount, CreatedAt, Price)
                              OUTPUT INSERTED.IdProductWarehouse
                              VALUES (@IdOrder, @IdProduct, @IdWarehouse, @Amount, @CreatedAt, @Price)";
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
            command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
            command.Parameters.AddWithValue("@Amount", product.Amount);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            command.Parameters.AddWithValue("@Price", finalPrice);

            int idProductWarehouse = (int)await command.ExecuteScalarAsync();
            await transaction.CommitAsync();
            return idProductWarehouse;
        }
        catch (Exception e)
        {
            transaction.Rollback();
            throw;
        }
        
    }

    public async Task<int> newProductWarehouseAddProcSkladowa(ProductWarehouseAdd product)
    {
        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();
            using (SqlCommand command = new SqlCommand("AddProductToWarehouse", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@IdProduct", product.IdProduct);
                command.Parameters.AddWithValue("@IdWarehouse", product.IdWarehouse);
                command.Parameters.AddWithValue("@Amount", product.Amount);
                command.Parameters.AddWithValue("@CreatedAt", product.CreatedAt);



                try
                {
                    var result = await command.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                    {
                        throw new Exception("Procedura nie zwróciła ID");
                    }

                    return Convert.ToInt32(result);
                }
                catch (SqlException ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }
    }

    public async Task<int> CzyIstniejeProdukt(int id)
    {
        var cmdText2 = @"select count(*) from Product where IdProduct = @IdProduct";

        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();

            using (SqlCommand command = new SqlCommand(cmdText2,connection))
            {
                command.Parameters.AddWithValue("@IdProduct", id);
                int result = (int)await command.ExecuteScalarAsync();
                return result;
            }
        }
    }

    public async Task<int> CzyIstniejeMagazyn(int id)
    {
        var cmdText2 = @"select count(*) from Warehouse where IdWarehouse = @IdWarehouse";

        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();

            using (SqlCommand command = new SqlCommand(cmdText2,connection))
            {
                command.Parameters.AddWithValue("@IdWarehouse", id);
                int result = (int)await command.ExecuteScalarAsync();
                return result;
            }
        }
    }

    public async Task<int> CzyIstniejeProduktwOrder(int id , int amount , DateTime createdAt)
    {
        var cmd = @"SELECT COUNT(*) 
                FROM [Order] 
                WHERE IdProduct = @idItem 
                AND Amount = @Amount 
                AND CreatedAt < @CreatedAt";

        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();
            using (SqlCommand command = new SqlCommand(cmd, connection))
            {
                
                command.Parameters.AddWithValue("@idItem", id);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@CreatedAt", createdAt);
                int result = (int)await command.ExecuteScalarAsync();
                return result;
            }
        }
    }

    public async Task<int> CzyZamówienieZrealizowane(int id, int amount)
    {
        var cmd = @"select COUNT(*)from Product_Warehouse pw
                    join [Order] O on O.IdOrder = pw.IdOrder
                    where o.IdProduct = @idItem and o.Amount = @Amount";
        
        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();
            using (SqlCommand command = new SqlCommand(cmd, connection))
            {
                command.Parameters.AddWithValue("@idItem", id);
                command.Parameters.AddWithValue("@Amount", amount);
                int result = (int)await command.ExecuteScalarAsync();
                return result;
            }
        }
        
    }

    
}