namespace backend.Dtos
{
    /// <summary>
    /// Data transfer object for event responses.
    /// </summary>
    public record EventDto(
        string Id,
        string Title,
        string? Subject,
        DateTime Start,
        DateTime End,
        string? Location,
        string Color
    );
}
