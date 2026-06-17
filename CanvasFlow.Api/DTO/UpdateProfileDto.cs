using System.ComponentModel.DataAnnotations;

namespace CanvasFlow.Api.DTO
{
    public class UpdateProfileDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;
    }
}
