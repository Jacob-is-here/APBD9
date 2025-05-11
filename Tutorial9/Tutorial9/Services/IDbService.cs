using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task DoSomethingAsync();
    Task ProcedureAsync();
    Task <int >newProductWarehouseAdd(ProductWarehouseAdd product);
    Task <int >newProductWarehouseAddProcSkladowa(ProductWarehouseAdd product);
    Task<int> CzyIstniejeProdukt(int id);
    Task<int> CzyIstniejeMagazyn(int id);
    Task<int> CzyIstniejeProduktwOrder(int id , int amount , DateTime createdAt);
    Task<int> CzyZamówienieZrealizowane(int id , int amount );
}