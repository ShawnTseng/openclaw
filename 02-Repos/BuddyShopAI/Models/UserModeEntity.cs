using Azure;
using Azure.Data.Tables;

namespace BuddyShopAI.Models;

public class UserModeEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "UserMode";

    public string RowKey { get; set; } = string.Empty;

    public string Mode { get; set; } = "ai";

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? Note { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

