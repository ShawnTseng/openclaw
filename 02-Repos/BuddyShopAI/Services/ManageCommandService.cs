using BuddyShopAI.Services;
using Line.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace BuddyShopAI.Services;

public class ManageCommandService
{
    private readonly ILogger<ManageCommandService> _logger;
    private readonly ConversationHistoryService _historyService;
    private readonly UserModeService _userModeService;
    private readonly PromptProvider _promptProvider;
    private readonly Kernel _kernel;
    private readonly ILineMessagingClient _lineClient;
    private readonly string _manageLineUserId;

    private const string AppVersion = "1.3.0";

    public ManageCommandService(
        ILogger<ManageCommandService> logger,
        ConversationHistoryService historyService,
        UserModeService userModeService,
        PromptProvider promptProvider,
        Kernel kernel,
        ILineMessagingClient lineClient,
        string manageLineUserId)
    {
        _logger = logger;
        _historyService = historyService;
        _userModeService = userModeService;
        _promptProvider = promptProvider;
        _kernel = kernel;
        _lineClient = lineClient;
        _manageLineUserId = manageLineUserId;
    }

    public bool IsManager(string userId) =>
        !string.IsNullOrEmpty(_manageLineUserId) && userId == _manageLineUserId;

    public bool IsManageCommand(string message) =>
        message.TrimStart().StartsWith("/manage", StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(string manageUserId, string rawMessage)
    {
        var parts = rawMessage.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var subCommand = parts.Length >= 2 ? parts[1].ToLower() : "help";

        _logger.LogInformation("Admin command received: {Command} from {ManageId}", rawMessage, manageUserId);

        string reply;
        if (subCommand == "ai" && parts.Length >= 3 && parts[2].Equals("last", StringComparison.OrdinalIgnoreCase))
        {
            reply = await HandleSetModeLastAsync();
        }
        else
        {
            reply = subCommand switch
            {
                "whoami"      => await HandleWhoAmIAsync(manageUserId),
                "status"      => await HandleStatusAsync(),
                "unanswered"  => await HandleUnansweredAsync(),
                "users"       => await HandleUsersAsync(),
                "modes"       => await HandleModesAsync(),
                "human"       => await HandleSetModeAsync(parts, "human"),
                "ai"          => await HandleSetModeAsync(parts, "ai"),
                "retry"       => await HandleRetryAsync(parts),
                "push"        => await HandlePushAsync(parts),
                "help"        => GetHelpText(),
                _             => $"❓ 未知指令：{subCommand}\n\n" + GetHelpText()
            };
        }

        ISendMessage outMessage;
        if (subCommand == "human" && parts.Length >= 3 && reply.StartsWith("🧑‍💼"))
        {
            var qr = new QuickReply
            {
                Items = new[]
                {
                    new QuickReplyButtonObject(new MessageTemplateAction("🤖 切回AI", $"/manage ai {parts[2]}"))
                }
            };
            outMessage = new TextMessage(reply, qr);
        }
        else
        {
            outMessage = new TextMessage(reply);
        }

        await _lineClient.PushMessageAsync(manageUserId, new[] { outMessage });
    }

    private Task<string> HandleWhoAmIAsync(string manageUserId)
    {
        return Task.FromResult(
            $"🪪 你的 LINE User ID：\n\n{manageUserId}\n\n" +
            "請將此 ID 填入環境變數：\nManage__LineUserId");
    }

    private async Task<string> HandleStatusAsync()
    {
        var modes = await _userModeService.GetAllUserModesAsync();
        var humanCount = modes.Count(u => u.Mode == "human");
        return $"✅ 系統運行中\n" +
               $"版號：{AppVersion}\n" +
               $"時間：{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
               $"👥 使用者統計\n" +
               $"• 真人模式：{humanCount} 人\n" +
               $"• AI 模式：{modes.Count - humanCount} 人（有記錄）";
    }

    private async Task<string> HandleUnansweredAsync()
    {
        var users = await _historyService.GetRecentUserLastMessagesAsync(48);
        var unanswered = users.Where(u => u.LastMessageRole == "user").ToList();

        if (unanswered.Count == 0)
            return "✅ 過去 48 小時內沒有未回覆的使用者";

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"⚠️ 未收到 AI 回應的使用者（{unanswered.Count} 人）：\n");
        foreach (var u in unanswered.Take(10))
        {
            lines.AppendLine($"👤 {u.UserId}");
            lines.AppendLine($"   最後訊息：{u.LastMessageContent}");
            lines.AppendLine($"   時間：{u.LastMessageTime:MM/dd HH:mm} UTC");
            lines.AppendLine();
        }
        if (unanswered.Count > 10)
            lines.AppendLine($"...以及其他 {unanswered.Count - 10} 人");

        lines.AppendLine("→ 回覆：/manage retry {userId}");
        return lines.ToString();
    }

    private async Task<string> HandleUsersAsync()
    {
        var users = await _historyService.GetRecentUserLastMessagesAsync(48);
        if (users.Count == 0)
            return "📭 過去 48 小時內沒有使用者互動";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"👥 最近 48h 活躍使用者（{users.Count} 人）：\n");
        foreach (var u in users.Take(15))
        {
            var icon = u.LastMessageRole == "user" ? "⚠️" : "✅";
            sb.AppendLine($"{icon} {u.UserId[..Math.Min(12, u.UserId.Length)]}...");
            sb.AppendLine($"   {u.LastMessageContent}");
            sb.AppendLine($"   {u.LastMessageTime:MM/dd HH:mm} | {u.TotalMessages} 則");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private async Task<string> HandleModesAsync()
    {
        var modes = await _userModeService.GetAllUserModesAsync();
        var human = modes.Where(m => m.Mode == "human").ToList();

        if (human.Count == 0)
            return "ℹ️ 目前沒有使用者在真人客服模式";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🧑‍💼 真人客服模式使用者（{human.Count} 人）：\n");
        foreach (var u in human)
        {
            sb.AppendLine($"• {u.RowKey}");
            if (!string.IsNullOrEmpty(u.Note))
                sb.AppendLine($"  備註：{u.Note}");
            sb.AppendLine($"  切換時間：{u.ChangedAt:MM/dd HH:mm}");
        }
        return sb.ToString();
    }

    private async Task<string> HandleSetModeLastAsync()
    {
        var modes = await _userModeService.GetAllUserModesAsync();
        var lastHuman = modes
            .Where(m => m.Mode == "human")
            .OrderByDescending(m => m.ChangedAt)
            .FirstOrDefault();

        if (lastHuman == null)
            return "ℹ️ 目前沒有使用者在真人客服模式";

        var targetUserId = lastHuman.RowKey;
        return await HandleSetModeAsync(new[] { "/manage", "ai", targetUserId }, "ai");
    }

    private async Task<string> HandleSetModeAsync(string[] parts, string mode)
    {
        if (parts.Length < 3)
            return $"❌ 用法：/manage {mode} {{userId}}";

        var targetUserId = parts[2];
        var note = parts.Length >= 4 ? string.Join(" ", parts[3..]) : null;

        try
        {
            await _userModeService.SetUserModeAsync(targetUserId, mode, note);
            var icon = mode == "human" ? "🧑‍💼" : "🤖";
            var label = mode == "human" ? "真人客服" : "AI 自動";
            return $"{icon} 已將使用者切換為{label}模式\n\nUserId：{targetUserId}" +
                   (note != null ? $"\n備註：{note}" : "");
        }
        catch (Exception ex)
        {
            return $"❌ 切換失敗：{ex.Message}";
        }
    }

    private async Task<string> HandleRetryAsync(string[] parts)
    {
        if (parts.Length < 3)
            return "❌ 用法：/manage retry {userId}";

        var targetUserId = parts[2];
        try
        {
            var systemPrompt = await _promptProvider.GetSystemPromptAsync();
            var history = await _historyService.GetChatHistoryAsync(targetUserId, systemPrompt);

            var conversationMessages = history
                .Where(m => m.Role == AuthorRole.User || m.Role == AuthorRole.Assistant)
                .ToList();

            if (conversationMessages.Count == 0)
                return $"❌ 找不到 {targetUserId} 的對話記錄";

            var lastMsg = conversationMessages.Last();
            string responseText;
            string action;

            if (lastMsg.Role == AuthorRole.Assistant && lastMsg.Content != null)
            {
                responseText = lastMsg.Content;
                action = "重送上一則 AI 回應";
            }
            else
            {
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                var aiResponse = await chatService.GetChatMessageContentAsync(history);
                responseText = aiResponse.Content ?? "抱歉，稍後再試。";
                await _historyService.SaveMessageAsync(targetUserId, "assistant", responseText);
                action = "重新生成 AI 回應";
            }

            await _lineClient.PushMessageAsync(targetUserId, new[] { new TextMessage(responseText) });

            return $"✅ {action} 成功\n\nUserId：{targetUserId}\n訊息長度：{responseText.Length} 字";
        }
        catch (Exception ex)
        {
            return $"❌ Retry 失敗：{ex.Message}";
        }
    }

    private async Task<string> HandlePushAsync(string[] parts)
    {
        if (parts.Length < 4)
            return "❌ 用法：/manage push {userId} {訊息內容}";

        var targetUserId = parts[2];
        var message = string.Join(" ", parts[3..]);

        try
        {
            await _historyService.SaveMessageAsync(targetUserId, "assistant", message);
            await _lineClient.PushMessageAsync(targetUserId, new[] { new TextMessage(message) });
            return $"✅ 訊息已傳送給使用者\n\nUserId：{targetUserId}\n內容：{message[..Math.Min(50, message.Length)]}...";
        }
        catch (Exception ex)
        {
            return $"❌ Push 失敗：{ex.Message}";
        }
    }

    private static string GetHelpText() =>
        "🛠️ Manage 指令說明\n\n" +
        "/manage whoami\n  → 查看自己的 LINE userId\n\n" +
        "/manage status\n  → 系統狀態與版號\n\n" +
        "/manage unanswered\n  → 未收到 AI 回應的使用者\n\n" +
        "/manage users\n  → 最近 48h 活躍使用者\n\n" +
        "/manage modes\n  → 目前真人模式的使用者\n\n" +
        "/manage human {userId}\n  → 切換為真人客服模式\n\n" +
        "/manage ai {userId}\n  → 切換回 AI 模式\n" +
        "/manage ai last\n  → ⚡ 切換最近一位真人用戶回 AI（最常用）\n\n" +
        "/manage retry {userId}\n  → 強制重送 AI 回應\n\n" +
        "/manage push {userId} {訊息}\n  → 以 AI 身份傳訊給使用者\n\n" +
        "/manage help\n  → 顯示此說明";
}

