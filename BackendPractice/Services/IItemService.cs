using BackendPractice.Models;

namespace BackendPractice.Services; 

public interface IItemService
{
    IReadOnlyCollection<Item> GetAll();
    Item? GetById(int id);
    Item Create(string name, string? description);
    Item? Update(int id, string name, string? description);
    bool Delete(int id);
}