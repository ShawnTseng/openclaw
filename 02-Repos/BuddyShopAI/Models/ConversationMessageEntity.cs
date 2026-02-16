using Azure;
using Azure.Data.Tables;

namespace BuddyShopAI.Models;

/// <summary>
/// Azure Table Storage entity for storing conversation messages
/// </summary>
public class ConversationMessageEntity : ITableEntity
{
    /// <summary>
    /// LINE User ID (Partition Key for efficient queries per user)
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp in format yyyyMMddHHmmssfff (Row Key for ordering)
    /// </summary>
    public string RowKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Message role: "user" or "assistant"
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Message content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp (Azure Table Storage managed)
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }
    
    /// <summary>
    /// ETag for concurrency control (Azure Table Storage managed)
    /// </summary>
    public ETag ETag { get; set; }
}
