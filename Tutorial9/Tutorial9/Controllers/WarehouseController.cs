using Microsoft.AspNetCore.Mvc;
using Tutorial9.Services;
using Tutorial9.Model;
namespace Tutorial9.Controllers;
[ApiController]
[Route("api/[controller]")]
public class WarehouseController: ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> Add(ProductWarehouseAdd product)
    {
        var zmienna = await _dbService.CzyIstniejeProdukt(product.IdProduct);
        if (zmienna == 0)
        {
            return NotFound($"Nie istnieje podany IdProduct {product.IdProduct}");
        }

        zmienna = await _dbService.CzyIstniejeMagazyn(product.IdWarehouse);
        if (zmienna == 0)
        {
            return NotFound($"Nie istnieje podany IdWarehouse {product.IdWarehouse}");
        }

        
        if (product.Amount <= 0)
        {
            return BadRequest("Amount musi być większe od 0");
        }
        
        zmienna = await _dbService.CzyIstniejeProduktwOrder(product.IdProduct, product.Amount , product.CreatedAt);
        if (zmienna == 0 )
        {
            return NotFound($"Nie istnieje podany IdProduct w Order {product.IdProduct} lub amount sie nie zgadza lub data jest zbyt wczesna");
        }


        zmienna = await _dbService.CzyZamówienieZrealizowane(product.IdProduct, product.Amount);
        if (zmienna > 0)
        {
            return BadRequest("Zamówienie zostało już zrealizowane");

        }

        var newProductWarehouseAdd = await _dbService.newProductWarehouseAdd(product);
        

        return Created("", new { IdProductWarehouse = newProductWarehouseAdd });    
    }
    
    [HttpPost("p")]
    public async Task<IActionResult> AddUsingStoredProcedure(ProductWarehouseAdd product)
    {
        try
        {
            var newId = await _dbService.newProductWarehouseAddProcSkladowa(product);
            return Created("", new { IdProductWarehouse = newId });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}