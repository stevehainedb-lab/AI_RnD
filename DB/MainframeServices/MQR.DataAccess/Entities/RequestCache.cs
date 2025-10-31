using System.ComponentModel.DataAnnotations;

namespace MQR.DataAccess.Entities;

[Obsolete("Do not use RequestCache directly. Use IDistributedCache instead.")]
public class RequestCache
{
    [Key]
    [MaxLength(449)]
    public required string Id { get; set; }
    
    public required byte[] Value { get; set; }

    public required DateTimeOffset ExpiresAtTime { get; set; }

    public long? SlidingExpirationInSeconds { get; set; }

    public DateTimeOffset? AbsoluteExpiration { get; set; }
}