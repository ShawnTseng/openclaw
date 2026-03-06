using Azure;
using Azure.Data.Tables;

namespace BuddyShopAI.Models;

public class ConversationMessageEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    
    public string RowKey { get; set; } = string.Empty;
    
    public string Role { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public DateTimeOffset? Timestamp { get; set; }
    
    public ETag ETag { get; set; }
}

