namespace Core.Dto;

public sealed record CustomerDto(
    string Id,
    string Name,
    string? Email = null);
