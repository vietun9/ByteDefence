namespace ByteDefence.Shared.DTOs;

public record SignalRMessage(string Method, string? Group, object? Data);
