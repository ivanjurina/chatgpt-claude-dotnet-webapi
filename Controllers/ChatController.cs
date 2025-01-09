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
    public async Task<IActionResult> StreamMessage(
        [FromBody] ChatRequest request,
        [FromQuery] string provider = "chatgpt")
    {
        try
        {
            var userId = int.Parse(_httpContextAccessor.HttpContext!.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userId == 0)
                return Unauthorized();

            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            await foreach (var chunk in _chatService.StreamChatAsync(userId, request, provider))
            {
                var data = $"data: {JsonSerializer.Serialize(chunk)}\n\n";
                await Response.WriteAsync(data);
                await Response.Body.FlushAsync();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
        }
    }
}