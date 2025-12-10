using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    public record UpdateProductDto(
        [Required] string Name,
        [Range(0.01, double.MaxValue)] double Price,
        [Range(0, int.MaxValue)] int Quantity
    );
}