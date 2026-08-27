using System.ComponentModel.DataAnnotations;

namespace WonjinApi.DTOs;

public record PushPublicKeyResponse(string PublicKey);

public record PushSubscribeRequest(
    [Required, MaxLength(500)] string Endpoint,
    [Required, MaxLength(200)] string P256dh,
    [Required, MaxLength(200)] string Auth
);

public record PushUnsubscribeRequest([Required, MaxLength(500)] string Endpoint);
