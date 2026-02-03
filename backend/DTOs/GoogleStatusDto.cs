using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class GoogleStatusDto
    {
        [Required]
        public required bool IsConnected { get; set; }
        public string? Email { get; set; }
    }
}
