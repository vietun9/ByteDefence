namespace ByteDefence.Shared.DTOs;

public record SignalRMessage(string Method, string? Group, object? Data);

public record OrderUpdatedMessage(string OrderId, string Action, object? Order);
