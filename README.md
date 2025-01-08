# ChatGPT & Claude .NET Web API

A robust .NET Web API project that demonstrates integration with both OpenAI's ChatGPT and Anthropic's Claude AI models. This project serves as a production-ready template for building AI-powered applications using modern .NET technologies.

## Features

- **AI Model Integration**
  - OpenAI ChatGPT integration with configurable parameters
  - Anthropic Claude 3 (Sonnet) integration with latest model support
  - Configurable token limits and system prompts
  - Error handling and retry logic for API calls

- **Security**
  - JWT Authentication with configurable settings
  - Secure password hashing using BCrypt
  - Role-based authorization
  - API key management
  - CORS policy configuration

- **Database**
  - Entity Framework Core with SQLite database
  - Code-first migrations
  - Repository pattern implementation
  - Efficient data access patterns

- **API Documentation**
  - Comprehensive Swagger/OpenAPI documentation
  - Detailed endpoint descriptions
  - Request/response examples
  - Authentication documentation

- **Best Practices**
  - Clean architecture principles
  - Dependency injection
  - Async/await patterns
  - Extensive error handling
  - Logging and monitoring

## Prerequisites

- .NET 9.0 SDK
- SQLite
- API keys for:
  - OpenAI (ChatGPT)
  - Anthropic (Claude)

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/chatgpt-claude-dotnet-webapi.git
   cd chatgpt-claude-dotnet-webapi
   ```

2. Update the configuration in `appsettings.json`:
   ```json
   {
     "JwtConfig": {
       "Secret": "your-super-secret-key-with-at-least-32-characters",
       "Issuer": "your-api",
       "Audience": "your-clients",
       "ExpiryInMinutes": 60
     },
     "ClaudeSettings": {
       "ApiKey": "your-anthropic-api-key-here",
       "Model": "claude-3-sonnet-20240229"
     },
     "ChatGptSettings": {
       "ApiKey": "your-openai-api-key-here"
     }
   }
   ```

3. Run the database migrations:
   ```bash
   dotnet ef database update
   ```

4. Build and run the application:
   ```bash
   dotnet build
   dotnet run
   ```

5. Access the Swagger documentation at `https://localhost:5001/swagger`

## Project Structure
The project follows a clean architecture pattern and is organized as follows:

### Core Components

- `Controllers/`: Contains API endpoints
  - `AuthController.cs`: Handles user authentication and registration
  - `ChatController.cs`: Manages chat interactions with AI models
  - `HealthController.cs`: Basic health check endpoint

- `Models/`: Data models and DTOs
  - `Auth/`: Authentication-related models
  - `Chat/`: Chat and message-related models
  - `Settings/`: Configuration model classes

- `Services/`: Business logic implementation
  - `IAuthService.cs` & `AuthService.cs`: User authentication logic
  - `IChatService.cs` & `ChatService.cs`: Chat processing logic
  - `ITokenService.cs` & `TokenService.cs`: JWT token management

- `Data/`: Database context and configurations
  - `ApplicationDbContext.cs`: EF Core database context
  - `Migrations/`: Database migration files

### Key Features

- JWT-based authentication
- Integration with multiple AI models:
  - OpenAI's ChatGPT
  - Anthropic's Claude
- SQLite database for data persistence
- Swagger/OpenAPI documentation
- Health check endpoints

### Configuration Files

- `appsettings.json`: Main application configuration
- `appsettings.Development.json`: Development-specific settings
- `Program.cs`: Application startup and DI configuration

### Security

- Password hashing using BCrypt
- JWT token authentication
- API key security for AI services

