using System.Text;
using chatgpt_claude_dotnet_webapi.DataModel;
using chatgpt_claude_dotnet_webapi.DataModel.Entities;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace chatgpt_claude_dotnet_webapi.Services;

public interface IDocumentService
{
    Task<Document> SaveAndProcessPdfAsync(int userId, int? chatId, IFormFile file);
    Task<Document> GetDocumentAsync(int userId, int documentId);
    Task<IEnumerable<Document>> GetUserDocumentsAsync(int userId);
    Task<IEnumerable<Document>> GetChatDocumentsAsync(int userId, int chatId);
    Task<(string fileName, string contentType, Stream fileStream)> GetDocumentContentAsync(int userId, int documentId);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;

    public DocumentService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _uploadPath = _configuration["DocumentStorage:Path"] ?? "Uploads/Documents";
        
        // Ensure upload directory exists
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<Document> SaveAndProcessPdfAsync(int userId, int? chatId, IFormFile file)
    {
        // Generate unique filename
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(_uploadPath, fileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Extract text
        string extractedText;
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            extractedText = await ExtractTextFromPdfAsync(stream);
        }

        // Create document record
        var document = new Document
        {
            UserId = userId,
            ChatId = chatId,
            FileName = file.FileName,
            StoragePath = filePath,
            ExtractedText = extractedText,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<Document> GetDocumentAsync(int userId, int documentId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

        if (document == null)
            throw new KeyNotFoundException("Document not found");

        return document;
    }

    public async Task<IEnumerable<Document>> GetUserDocumentsAsync(int userId)
    {
        return await _context.Documents
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Document>> GetChatDocumentsAsync(int userId, int chatId)
    {
        return await _context.Documents
            .Where(d => d.UserId == userId && d.ChatId == chatId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<(string fileName, string contentType, Stream fileStream)> GetDocumentContentAsync(int userId, int documentId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

        if (document == null)
            throw new KeyNotFoundException("Document not found");

        if (!System.IO.File.Exists(document.StoragePath))
            throw new FileNotFoundException("Document file not found on server");

        var fileStream = new FileStream(document.StoragePath, FileMode.Open, FileAccess.Read);
        var contentType = "application/pdf";
        
        return (document.FileName, contentType, fileStream);
    }

    private async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        using var pdfReader = new PdfReader(pdfStream);
        using var pdfDocument = new PdfDocument(pdfReader);
        var text = new StringBuilder();

        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            var page = pdfDocument.GetPage(i);
            text.Append(PdfTextExtractor.GetTextFromPage(page));
            text.Append("\n\n"); // Add spacing between pages
        }

        return text.ToString();
    }
} 