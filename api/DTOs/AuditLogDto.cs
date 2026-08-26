namespace WonjinApi.DTOs;

public record AuditLogDto(
    long Id, int? ActorUserId, string ActorEmail, string ActorRole,
    string Action, string EntityType, string? EntityId, string Summary,
    string? Ip, int StatusCode, DateTimeOffset CreatedAt
);
