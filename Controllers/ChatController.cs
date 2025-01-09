using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.Services;
using System.Security.Claims;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using System.Text.Json;

namespace chatgpt_claude_dotnet_webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatController(
        IChatService chatService,
        IHttpContextAccessor httpContextAccessor)
    {
        _chatService = chatService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("message")]
    public async Task<ActionResult<ChatResponse>> SendMessage(
        [FromBody] ChatRequest request,
        [FromQuery] string provider = "chatgpt")
    {
        try
        {
            var userId = int.Parse(_httpContextAccessor.HttpContext!.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            var response = await _chatService.ChatAsync(userId, request, provider);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
        }
    }

    [HttpGet("{chatId}")]
    public async Task<ActionResult<Chat>> GetChat(int chatId)
    {
        try
        {
            var userId = int.Parse(_httpContextAccessor.HttpContext!.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            var chat = await _chatService.GetChatHistoryAsync(userId, chatId);
            return Ok(chat);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving chat history.", error = ex.Message });
        }
    }

    [HttpGet("")]
    public async Task<ActionResult<IEnumerable<Chat>>> GetUserChats()
    {
        try
        {
            var userId = int.Parse(_httpContextAccessor.HttpContext!.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            var chats = await _chatService.GetUserChatsAsync(userId);
            return Ok(chats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving chats.", error = ex.Message });
        }
    }

    [HttpPost("message/stream")]
    public async Task StreamMessage(
        [FromBody] ChatRequest request,
        [FromQuery] string provider = "chatgpt",
        CancellationToken cancellationToken = default)
    {
        var userId = int.Parse(_httpContextAccessor.HttpContext!.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        if (userId == 0)
        {
            Response.StatusCode = 401;
            return;
        }

        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no");

        try
        {
            var stream = _chatService.StreamChatAsync(userId, request, provider);
            
            await foreach (var chunk in stream.WithCancellation(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) break;

                var data = JsonSerializer.Serialize(new
                {
                    message = chunk.Message,
                    chatId = chunk.ChatId,
                    isComplete = chunk.IsComplete,
                    isNewChat = request.ChatId == null
                });
                
                await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (!Response.HasStarted)
            {
                Response.StatusCode = 500;
                await Response.WriteAsJsonAsync(new 
                { 
                    message = "An error occurred while processing your request.", 
                    error = ex.Message 
                }, cancellationToken);
            }
        }
    }
}