using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.Services;
using System.Text;
using System.Text.Json;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SpeechController : ControllerBase
    {
        private readonly ISpeechService _speechService;

        public SpeechController(ISpeechService speechService)
        {
            _speechService = speechService;
        }

        [HttpPost("transcribe")]
        public async Task StreamTranscription(
            IFormFile audioFile,
            CancellationToken cancellationToken)
        {
            try
            {
                if (audioFile.Length == 0)
                {
                    Response.StatusCode = 400;
                    await Response.WriteAsJsonAsync(new { error = "Audio file is empty" });
                    return;
                }

                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                using var stream = audioFile.OpenReadStream();
                var transcribedText = new StringBuilder();

                await foreach (var textChunk in _speechService.StreamSpeechToTextAsync(stream)
                    .WithCancellation(cancellationToken))
                {
                    transcribedText.Append(textChunk);
                    var data = JsonSerializer.Serialize(new
                    {
                        text = textChunk,
                        isComplete = false
                    });
                    await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }

                // Send final complete message
                var finalData = JsonSerializer.Serialize(new
                {
                    text = transcribedText.ToString(),
                    isComplete = true
                });
                await Response.WriteAsync($"data: {finalData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 500;
                    await Response.WriteAsJsonAsync(new { error = ex.Message });
                }
            }
        }
    }
} 