using System.ComponentModel.DataAnnotations;

namespace BackendPractice.Models;

public sealed class ItemRequest
{
    [Required]
    [StringLength(100)]
    public string Name {get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description {get; init;} 
}