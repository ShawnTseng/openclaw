using Azure;
using Azure.Data.Tables;

namespace BuddyShopAI.Models;

/// <summary>
/// Azure Table Storage entity for buffering rapid-fire messages before AI processing.
/// PartitionKey = userId, RowKey = timestamp (yyyyMMddHHmmssfff)
/// </summary>
public class PendingMessageEntity : ITableEntity
{
    /// <summary>
    /// LINE User ID
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp in format yyyyMMddHHmmssfff (for ordering)
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// LINE ReplyToken (only the latest one is usable)
    /// </summary>
    public string ReplyToken { get; set; } = string.Empty;

    /// <summary>
    /// Azure Table Storage managed timestamp
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// ETag for concurrency control
    /// </summary>
    public ETag ETag { get; set; }
}
