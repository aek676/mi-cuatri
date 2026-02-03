using System.ComponentModel.DataAnnotations;

public class GoogleConnectResponse
{
    [Required]
    public required Uri Url { get; set; }
    [Required]
    public required string StateToken { get; set; }
}