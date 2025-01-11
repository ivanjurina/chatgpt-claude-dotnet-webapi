using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using chatgpt_claude_dotnet_webapi.Contracts;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using Newtonsoft.Json;
using System.Security.Claims;
using chatgpt_claude_dotnet_webapi.Services;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService documentService,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Document>> UploadPdf(
            IFormFile file,
            [FromQuery] int? chatId = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("File must be a PDF");

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var document = await _documentService.SaveAndProcessPdfAsync(userId, chatId, file);
                
                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF");
                return StatusCode(500, new { message = "Error processing PDF", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetDocument(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var document = await _documentService.GetDocumentAsync(userId, id);
                return Ok(document);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document");
                return StatusCode(500, new { message = "Error retrieving document", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetUserDocuments()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var documents = await _documentService.GetUserDocumentsAsync(userId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new { message = "Error retrieving documents", error = ex.Message });
            }
        }

        [HttpGet("chat/{chatId}")]
        public async Task<ActionResult<IEnumerable<Document>>> GetChatDocuments(int chatId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var documents = await _documentService.GetChatDocumentsAsync(userId, chatId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat documents");
                return StatusCode(500, new { message = "Error retrieving chat documents", error = ex.Message });
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var (fileName, contentType, fileStream) = await _documentService.GetDocumentContentAsync(userId, id);

                // Return file with using statement to ensure proper disposal
                return new FileStreamResult(fileStream, contentType)
                {
                    FileDownloadName = fileName,
                    EnableRangeProcessing = true // Enables resume and streaming support
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (FileNotFoundException)
            {
                return StatusCode(500, new { message = "Document file not found on server" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document");
                return StatusCode(500, new { message = "Error downloading document", error = ex.Message });
            }
        }
    }
} 