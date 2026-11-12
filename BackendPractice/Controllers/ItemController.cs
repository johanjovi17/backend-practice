using BackendPractice.Models;
using BackendPractice.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendPractice.controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService){
        _itemService = itemService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<Item>> GetAll() => Ok(_itemService.GetAll());

    [HttpGet("{id:int}")]
    public ActionResult<Item> GetById(int id){
        var item = _itemService.GetById(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public ActionResult<Item> Create(ItemRequest request){
        var item = _itemService.Create(request.Name,request.Description);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item); 
    }
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, ItemRequest request){
        var item = _itemService.Update(id, request.Name, request.Description);
        return item is null ? NotFound() : NoContent();
    }
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)=> _itemService.Delete(id) ? NoContent() : NotFound();
}