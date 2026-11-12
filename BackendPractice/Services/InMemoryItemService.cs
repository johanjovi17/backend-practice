using BackendPractice.Models;
namespace BackendPractice.Services;

public sealed class InMemoryItemService : IItemService
{
    private readonly Dictionary<int, Item> _items = [];
    private readonly object _lock = new();
    private int _nextId;

    public IReadOnlyCollection<Item> GetAll(){
        lock(_lock){
        return _items.Values.OrderBy(item => item.Id).ToArray();
        }
    }

    public Item? GetById(int id){
        lock(_lock){
            return _items.GetValueOrDefault(id);
        }
    }

    public Item Create(string name, string? description){
        lock(_lock){
            var item = new Item(++_nextId, name, description);
            _items[item.Id] = item;
            return item;
        }
    }

    public Item? Update(int id, string name, string? description){
        lock(_lock){
            if(!_items.ContainsKey(id)) return null;

            var item = new Item(id,name,description);
            _items[id] = item;
            return item;
        }
    }

        public bool Delete(int id){
            lock(_lock){
                return _items.Remove(id);
            }
        }
    }
    
