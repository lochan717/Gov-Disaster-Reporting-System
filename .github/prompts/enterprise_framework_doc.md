# Enterprise Application Development Framework

## Table of Contents
1. [Introduction](#introduction)
2. [Solution Architecture](#solution-architecture)
3. [Project Structure](#project-structure)
4. [Tech Stack](#tech-stack)
5. [Development Standards](#development-standards)
6. [Security Implementation](#security-implementation)
7. [Data Access Pattern](#data-access-pattern)
8. [API Communication](#api-communication)
9. [UI Components](#ui-components)
10. [Error Handling](#error-handling)
11. [ASP.NET Core Identity Integration](#aspnet-core-identity-integration)
12. [Deployment](#deployment)

## Introduction

This framework provides development standards, architectural patterns, and best practices for enterprise applications. It ensures consistency across all aspects of application development and is suitable for business process automation, document management, and workflow applications.

## Solution Architecture

The solution follows Clean Architecture with clear separation of concerns:

```
EnterpriseApp.sln
├── EnterpriseApp.Domain (Core entities and business rules)
├── EnterpriseApp.Application (Business logic and interfaces)
├── EnterpriseApp.Infrastructure (Data access, external services)
├── EnterpriseApp.Api (Web API endpoints)
├── EnterpriseApp.Web (MVC Web application)
├── EnterpriseApp.Shared (Shared models and utilities)
├── EnterpriseApp.External (External service integrations)
```

### Important Notes
- **NO Repository Pattern**: Use Services directly instead of repositories
- **Default Database Connection**: Use `SULOCHANNEW\\SQLEXPRESS` server with username=`sa` and password=`sa_123`
- ** Use ASp.NET Core Identity in API for userManagement and JWT Authentication and Cookie Authentication in WEB
- ** Always use PartialViews and asynchronous approach for any form add,edit,delete,list,views,paging etc.
### Architectural Flow

1. **Presentation Layer** (Web and API projects) - Handles requests, validates input, calls services
2. **Application Layer** (Application project) - Contains business logic and service interfaces  
3. **Domain Layer** (Domain project) - Core business entities, independent of external concerns
4. **Infrastructure Layer** (Infrastructure and External projects) - Data access and external services
5. **Shared Layer** (Shared project) - DTOs, constants, and utilities used across projects

## Project Structure

### EnterpriseApp.Domain

Contains core business entities, enumerations, and domain logic with no dependencies.

```
EnterpriseApp.Domain/
├── Constants/
├── Entities/
│   ├── ApplicationUser.cs
│   ├── Document.cs
│   ├── DocumentHistory.cs
│   ├── Department.cs
│   └── AuditLog.cs
├── Interfaces/
└── EnterpriseApp.Domain.csproj
```

#### Key Domain Entities

**ApplicationUser**: Extends IdentityUser with additional properties
Example:
```csharp
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }
    public string DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastLogin { get; set; }
    
    // Navigation properties
    public virtual Department Department { get; set; }
}
```

**Document**: Represents documents with tracking properties
Example:
```csharp
public class Document
{
    public string Id { get; set; }
    public string DocumentNumber { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DocumentStatus Status { get; set; }
    public DocumentPriority Priority { get; set; }
    public bool IsConfidential { get; set; }
    public string FilePath { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedById { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public string LastModifiedById { get; set; }
    public string AssignedToId { get; set; }
    public string DepartmentId { get; set; }
    
    // Navigation properties
    public ApplicationUser CreatedBy { get; set; }
    public ApplicationUser LastModifiedBy { get; set; }
    public ApplicationUser AssignedTo { get; set; }
    public Department Department { get; set; }
    public ICollection<DocumentHistory> History { get; set; }
}
```

### EnterpriseApp.Application

Contains business logic, service interfaces, and DTOs. Depends only on Domain project.

```
EnterpriseApp.Application/
├── DependencyInjection.cs
├── DTOs/
│   ├── Audit/
│   ├── Dashboard/
│   ├── Document/
Example:
│   │   ├── DocumentRequestDto.cs
│   │   ├── DocumentResponseDto.cs
│   │   └── DocumentListItemDto.cs
│   └── User/
Example:
│       ├── LoginRequestDto.cs
│       ├── LoginResponseDto.cs
│       └── CreateUserDto.cs
├── Services/
Example:
│   ├── AuditService.cs
│   ├── AuthenticationService.cs
│   ├── DocumentService.cs
│   └── UserService.cs
└── EnterpriseApp.Application.csproj
```

#### Service Implementation Pattern
Example:
```csharp
public class DocumentService : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    
    public DocumentService(IUnitOfWork unitOfWork, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }
    
    public async Task<Result<DocumentResponseDto>> GetDocumentByIdAsync(string id)
    {
        var document = await _unitOfWork.Documents.GetByIdAsync(id);
        if (document == null)
            return Result<DocumentResponseDto>.Failure("Document not found");
            
        // Manual mapping (No AutoMapper)
        var documentDto = new DocumentResponseDto
        {
            Id = document.Id,
            DocumentNumber = document.DocumentNumber,
            Title = document.Title,
            Description = document.Description,
            Status = document.Status.ToString(),
            Priority = document.Priority.ToString(),
            IsConfidential = document.IsConfidential,
            CreatedOn = document.CreatedOn,
            AssignedToName = document.AssignedTo?.FullName,
            DepartmentName = document.Department?.Name
        };
        
        return Result<DocumentResponseDto>.Success(documentDto);
    }
    
    public async Task<Result<DocumentResponseDto>> CreateDocumentAsync(DocumentRequestDto requestDto, string userId)
    {
        var document = new Document
        {
            Id = Guid.NewGuid().ToString(),
            DocumentNumber = requestDto.DocumentNumber,
            Title = requestDto.Title,
            Description = requestDto.Description,
            Status = DocumentStatus.New,
            Priority = requestDto.Priority,
            IsConfidential = requestDto.IsConfidential,
            CreatedOn = DateTime.UtcNow,
            CreatedById = userId,
            DepartmentId = requestDto.DepartmentId
        };
        
        await _unitOfWork.Documents.AddAsync(document);
        
        // Log audit event
        await _auditService.LogAuditAsync(
            userId, "Create", "Document", document.Id, 
            null, JsonSerializer.Serialize(document));
        
        await _unitOfWork.SaveChangesAsync();
        
        var responseDto = new DocumentResponseDto
        {
            Id = document.Id,
            DocumentNumber = document.DocumentNumber,
            Title = document.Title,
            Status = document.Status.ToString()
        };
        
        return Result<DocumentResponseDto>.Success(responseDto);
    }
}
```

### EnterpriseApp.Infrastructure

Handles data access and persistence. Depends on Domain and Application projects.
Example:
```
EnterpriseApp.Infrastructure/
├── DependencyInjection.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   │   ├── ApplicationUserConfiguration.cs
│   │   ├── DocumentConfiguration.cs
│   │   └── DocumentHistoryConfiguration.cs
│   └── SeedData.cs
├── Migrations/
├── Services/
│   ├── EmailService.cs
│   └── CurrentUserService.cs
├── UnitOfWork/
│   └── UnitOfWork.cs
└── EnterpriseApp.Infrastructure.csproj
```

#### Entity Framework Configuration
Example:
```csharp
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.DocumentNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.HasOne(d => d.AssignedTo)
            .WithMany()
            .HasForeignKey(d => d.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(d => d.CreatedBy)
            .WithMany()
            .HasForeignKey(d => d.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### EnterpriseApp.Api

Exposes RESTful API endpoints. Depends on Application and Infrastructure projects.
Example:
```
EnterpriseApp.Api/
├── appsettings.json
├── Program.cs
├── Controllers/
│   ├── AuthenticationController.cs
│   ├── DashboardController.cs
│   ├── DocumentsController.cs
│   └── UsersController.cs
└── Helper/
```

#### Controller Implementation
Example:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;
    
    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponseDto<DocumentResponseDto>>> GetDocument(string id)
    {
        try
        {
            var result = await _documentService.GetDocumentByIdAsync(id);
            
            if (!result.Success)
                return NotFound(new ApiResponseDto<DocumentResponseDto> 
                { 
                    Success = false, 
                    Message = result.Message 
                });
                
            return Ok(new ApiResponseDto<DocumentResponseDto> 
            { 
                Success = true, 
                Data = result.Data 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
            return StatusCode(500, new ApiResponseDto<DocumentResponseDto> 
            { 
                Success = false, 
                Message = "An error occurred while retrieving the document" 
            });
        }
    }
    
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponseDto<DocumentResponseDto>>> CreateDocument(DocumentRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponseDto<DocumentResponseDto> 
                { 
                    Success = false, 
                    Message = "Invalid request data" 
                });
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _documentService.CreateDocumentAsync(request, userId);
            
            if (!result.Success)
                return BadRequest(new ApiResponseDto<DocumentResponseDto> 
                { 
                    Success = false, 
                    Message = result.Message 
                });
                
            return CreatedAtAction(nameof(GetDocument), new { id = result.Data.Id }, 
                new ApiResponseDto<DocumentResponseDto> 
                { 
                    Success = true, 
                    Data = result.Data 
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return StatusCode(500, new ApiResponseDto<DocumentResponseDto> 
            { 
                Success = false, 
                Message = "An error occurred while creating the document" 
            });
        }
    }
}
```

### EnterpriseApp.Web

MVC web application serving as the user interface. Depends on Shared project.
Example:
```
EnterpriseApp.Web/
├── appsettings.json
├── Program.cs
├── Controllers/
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   ├── DocumentAdminController.cs
│   ├── DocumentsController.cs
│   └── UsersController.cs
├── ApiClients/
│   └── HttpApiClient.cs
├── Views/
│   ├── Account/
│   ├── Dashboard/
│   ├── DocumentAdmin/
│   ├── Documents/
│   ├── Shared/
│   └── Users/
└── wwwroot/
    ├── css/
    ├── js/
    └── lib/
```

**Important**: 
- **NO ViewModels or Models** - Use DTOs from `.Application` or `.Shared` only
- Take full advantage of client-side rendering with jQuery Ajax and partial views
- Web Controllers handle all requests and use HttpApiClient to communicate with API
- Use MVC Controller Pattern for all requests and responses

#### MVC Controller Pattern
Example:
```csharp
public class DocumentAdminController : Controller
{
    private readonly IHttpService _httpService;
    private readonly ILogger<DocumentAdminController> _logger;
    
    public DocumentAdminController(IHttpService httpService, ILogger<DocumentAdminController> logger)
    {
        _httpService = httpService;
        _logger = logger;
    }
    
    [Authorize(Roles = "Admin")]
    public IActionResult Index()
    {
        return View();
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromBody] DocumentFilterDto filter)
    {
        try
        {
            var response = await _httpService.PostAsync<ApiResponseDto<PaginatedResult<DocumentListItemDto>>>(
                "api/documents/list", filter);
                
            if (response.Success && response.Data != null)
                return Json(new { success = true, data = response.Data });
            
            return Json(new { success = false, message = response.Message ?? "Failed to retrieve documents" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return Json(new { success = false, message = "An error occurred while retrieving documents." });
        }
    }
    
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Add()
    {
        return PartialView("_Add");
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Add(DocumentRequestDto model)
    {
        try
        {
            if (!ModelState.IsValid)
                return PartialView("_Add", model);
            
            var response = await _httpService.PostAsync<ApiResponseDto<DocumentResponseDto>>(
                "api/documents", model);
                
            if (response.Success)
                return Json(new { success = true, message = "Document created successfully" });
            
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to create document");
            return PartialView("_Add", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the document");
            return PartialView("_Add", model);
        }
    }
}
```

### EnterpriseApp.Shared

Contains shared models, constants, and utilities with no dependencies.
Example:
```csharp
public class Result<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    
    public static Result<T> Success(T data, string message = null)
    {
        return new Result<T> { Success = true, Message = message, Data = data };
    }
    
    public static Result<T> Failure(string message)
    {
        return new Result<T> { Success = false, Message = message };
    }
}

public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    
    public PaginatedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
```

### EnterpriseApp.External

Handles external service integrations. Depends on Application project.

## Tech Stack

### Backend
- **.NET 8** - Core framework
- **ASP.NET Core MVC** - Web application framework
- **ASP.NET Core Web API** - API framework
- **Entity Framework Core** - ORM for data access
- **ASP.NET Core Identity** - Authentication and user management
- **SQL Server** - Database

### Frontend
- **Tailwind CSS** - CSS framework
- **jQuery** - JavaScript library
- **Font Awesome** - Icon library
- **Toastr** - Notification library
- **jQuery Validation** - Client-side validation

### Development Tools
- **Visual Studio 2022** - IDE
- **Git** - Version control
- **Azure DevOps** - CI/CD pipeline

## Development Standards

### Core Principles
1. **Clean Code** - Meaningful names, small focused methods, self-documenting code
2. **DRY Principle** - Extract common functionality, use shared libraries
3. **Proper Code Organization** - Follow established project structure

### Specific Standards

#### No MediatR
Use direct service injection instead of MediatR:
Example:
```csharp
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    
    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponseDto<DocumentResponseDto>>> GetDocument(string id)
    {
        var result = await _documentService.GetDocumentByIdAsync(id);
        // Handle result
    }
}
```

#### No AutoMapper
Use manual mapping for better control and visibility:
Example:
```csharp
public async Task<Result<DocumentResponseDto>> GetDocumentByIdAsync(string id)
{
    var document = await _documentService.GetByIdAsync(id);
    if (document == null)
        return Result<DocumentResponseDto>.Failure("Document not found");
        
    // Manual mapping
    var documentDto = new DocumentResponseDto
    {
        Id = document.Id,
        DocumentNumber = document.DocumentNumber,
        Title = document.Title,
        Description = document.Description,
        Status = document.Status.ToString(),
        CreatedOn = document.CreatedOn,
        CreatedBy = document.CreatedBy?.FullName
    };
    
    return Result<DocumentResponseDto>.Success(documentDto);
}
```

#### Validation Standards
- **Client-Side**: jQuery Unobtrusive Validation
- **Server-Side**: Model validation with Data Annotations and ModelState
Example:
```csharp
public class CreateUserDto
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    public string Username { get; set; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Password must include uppercase, lowercase, number, and special character")]
    public string Password { get; set; }
}
```

#### External CSS & JS Only
- No inline or internal CSS/JS
- All styles and scripts in external files
Example:
```html
@section Styles {
    <link rel="stylesheet" href="~/css/document-details.css" />
}

<div class="document-container">
    <h2>@Model.Title</h2>
    <!-- Content -->
</div>

@section Scripts {
    <script src="~/js/document-management.js"></script>
    <script>
        $(document).ready(function() {
            documentManagement.initDetails('@Model.Id');
        });
    </script>
}
```

#### HttpService for API Communication

Centralized HTTP service for consistent API communication:
Example:
```csharp
public class HttpService : IHttpService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpService> _logger;
    
    public HttpService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<HttpService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    public async Task<T> GetAsync<T>(string endpoint)
    {
        try
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync(endpoint);
            await HandleResponseErrors(response);
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GET request to {Endpoint}", endpoint);
            throw;
        }
    }
    
    public async Task<T> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            AddAuthorizationHeader();
            
            var jsonContent = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(endpoint, content);
            
            await HandleResponseErrors(response);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in POST request to {Endpoint}", endpoint);
            throw;
        }
    }
    
    private void AddAuthorizationHeader()
    {
        var token = _httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
    
    private async Task HandleResponseErrors(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Unauthorized access");
            
            throw new ApiException($"API error: {response.StatusCode}", (int)response.StatusCode, content);
        }
    }
}
```

## Security Implementation

### Authentication & Authorization

#### JWT Authentication for API
Example:
```csharp
public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
{
    var roles = await _userManager.GetRolesAsync(user);
    
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("FullName", user.FullName)
    };
    
    foreach (var role in roles)
        claims.Add(new Claim(ClaimTypes.Role, role));
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddHours(1),
        signingCredentials: creds);
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

#### Cookie Authentication for Web
Example:
```csharp
public async Task<IActionResult> Login(LoginRequestDto model)
{
    if (!ModelState.IsValid)
        return View(model);
    
    var response = await _httpService.PostAsync<ApiResponseDto<LoginResponseDto>>("api/authentication/login", model);
    
    if (response.Success)
    {
        var userData = response.Data;
        
        // Store token in HttpOnly cookie
        Response.Cookies.Append("AuthToken", userData.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.Now.AddHours(1)
        });
        
        // Create claims identity for cookie authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userData.Username),
            new Claim(ClaimTypes.NameIdentifier, userData.Id),
            new Claim("FullName", userData.FullName)
        };
        
        foreach (var role in userData.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
        return RedirectToAction("Index", "Dashboard");
    }
    
    ModelState.AddModelError(string.Empty, response.Message);
    return View(model);
}
```

#### Role-Based Authorization
Example:
```csharp
// Authorization policies
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManageUsers", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManageDocuments", policy => policy.RequireRole("Admin", "ManagerUser"));
    options.AddPolicy("ViewDocuments", policy => policy.RequireRole("Admin", "ManagerUser", "StandardUser"));
});

// Usage in controllers
[Authorize(Policy = "ManageUsers")]
public class UsersController : Controller { }

[Authorize(Policy = "ViewDocuments")]
public class DocumentsController : Controller
{
    [HttpPost]
    [Authorize(Policy = "ManageDocuments")]
    public async Task<IActionResult> UpdateStatus(UpdateStatusDto model) { }
}
```

### CSRF Protection
Example:
```csharp
// Configuration
services.AddAntiforgery(options => 
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "CSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Usage in forms
<form asp-action="Create" method="post">
    @Html.AntiForgeryToken()
    <!-- Form fields -->
</form>

// Controller
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateDocumentDto model) { }
```

### XSS Prevention
Example:
```csharp
// Content Security Policy
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' https://cdn.jsdelivr.net;");
    await next();
});

// In views - automatic HTML encoding
<p>@Model.Description</p>

// JavaScript - proper encoding
function showDocumentDetails(document) {
    const title = $('<div>').text(document.title).html();
    $('#documentTitle').html(title);
}
```

### SQL Injection Prevention
Example:
```csharp
// Safe EF Core queries (parameters automatically sanitized)
public async Task<Document> GetByDocumentNumberAsync(string documentNumber)
{
    return await _context.Documents
        .FirstOrDefaultAsync(d => d.DocumentNumber == documentNumber);
}

// Raw SQL with parameters (when necessary)
public async Task<IEnumerable<Document>> GetDocumentsByStatusAsync(string status)
{
    return await _context.Documents
        .FromSqlRaw("SELECT * FROM Documents WHERE Status = {0}", status)
        .ToListAsync();
}
```

## Data Access Pattern

### Unit of Work Pattern
Example:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    
    public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict occurred while saving changes");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "An error occurred while saving changes");
            throw;
        }
    }
    
    public void Dispose() => _context.Dispose();
}
```

### Entity Framework Core Migrations
Example:
```bash
# Add migration
dotnet ef migrations add MigrationName --project EnterpriseApp.Infrastructure --startup-project EnterpriseApp.Api

# Update database
dotnet ef database update --project EnterpriseApp.Infrastructure --startup-project EnterpriseApp.Api
```
### Mistakes to avoid in Entity Framework
- Loading Entire Entities - Use Select() Projections
- Tracking Everything - Use AsNoTracking() for reads
- N+1 Queries in Loops - Include() or split queries
- ToList() Too Early - Keep IQueryable as long as possible
- Missing Indexes - .HasIndex() in OnModelCreating
- One Giant DbContext - Split by Bounded Context
- Ignoring Compiled Queries - EF.Compile AsyncQuery()
- Not Reading the SQL - .ToQuerySting()
- Overusing Include() (Cartesian Explosion)
- No Pagination
- Ignoring Query Reuse


## API Communication

### Request-Response Pattern
Example:
```csharp
// Consistent API response format
public class ApiResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}

// Controller implementation
[HttpGet("{id}")]
public async Task<ActionResult<ApiResponseDto<DocumentResponseDto>>> GetDocument(string id)
{
    try
    {
        var result = await _documentService.GetDocumentByIdAsync(id);
        
        if (!result.Success)
            return NotFound(new ApiResponseDto<DocumentResponseDto> 
            { 
                Success = false, 
                Message = result.Message 
            });
            
        return Ok(new ApiResponseDto<DocumentResponseDto> 
        { 
            Success = true, 
            Data = result.Data 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
        return StatusCode(500, new ApiResponseDto<DocumentResponseDto> 
        { 
            Success = false, 
            Message = "An error occurred while retrieving the document" 
        });
    }
}
```

### HttpService for Web to API Communication
Example:
```csharp
// Web controller using HttpService
public async Task<IActionResult> GetDocument(string id)
{
    try
    {
        var response = await _httpService.GetAsync<ApiResponseDto<DocumentResponseDto>>($"api/documents/{id}");
        
        if (response.Success)
            return View(response.Data);
        
        return NotFound();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
        return RedirectToAction("Error", "Home");
    }
}
```

## UI Components

### Split Pane Layout for Admin Functions

Admin functions use a split pane layout for better user experience:
Example:
```html
<div class="split-container">
    <!-- Main Content Panel -->
    <div id="mainContent" class="main-content">
        <h2>Document Management</h2>
        
        <div class="filters-container mb-3">
            <div class="row">
                <div class="col-md-3">
                    <input type="text" id="searchTerm" class="form-control" placeholder="Search...">
                </div>
                <div class="col-md-2">
                    <select id="statusFilter" class="form-select">
                        <option value="">- Status -</option>
                        <option value="New">New</option>
                        <option value="InProgress">In Progress</option>
                        <option value="Completed">Completed</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <button id="searchBtn" class="btn btn-primary w-100">Search</button>
                </div>
            </div>
        </div>
        
        <div class="table-responsive">
            <table id="documentsTable" class="table table-striped">
                <thead>
                    <tr>
                        <th>Document #</th>
                        <th>Title</th>
                        <th>Status</th>
                        <th>Priority</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    <!-- Data loaded via AJAX -->
                </tbody>
            </table>
        </div>
        
        <div class="pagination-container mt-3">
            <nav aria-label="Page navigation">
                <ul id="pagination" class="pagination justify-content-center">
                    <!-- Pagination loaded via JS -->
                </ul>
            </nav>
        </div>
    </div>
    
    <!-- Side Panel for add/edit forms -->
    <div id="sidePane" class="side-pane d-none">
        <div class="side-pane-header">
            <h5 id="sidePaneTitle">Form Title</h5>
            <button type="button" class="btn-close" id="closeSidePaneBtn"></button>
        </div>
        <div id="sidePaneContent" class="side-pane-content">
            <!-- Form content loaded dynamically -->
        </div>
    </div>
</div>
```

### JavaScript Module Pattern

Client-side functionality organized using the module pattern:
Example:
```javascript
const documentManagement = (function() {
    // Private variables
    let currentPage = 1;
    let pageSize = 10;
    let totalPages = 0;
    let currentFilters = {
        searchTerm: '',
        status: '',
        priority: '',
        departmentId: ''
    };
    
    // Private functions
    function renderTable(documents) {
        const $tbody = $('#documentsTable tbody');
        $tbody.empty();
        
        if (!documents || documents.length === 0) {
            $tbody.html('<tr><td colspan="5" class="text-center">No documents found</td></tr>');
            return;
        }
        
        const rows = documents.map(doc => `
            <tr>
                <td>${doc.documentNumber}</td>
                <td>${doc.title}</td>
                <td><span class="badge bg-${getStatusBadgeClass(doc.status)}">${doc.status}</span></td>
                <td><span class="badge bg-${getPriorityBadgeClass(doc.priority)}">${doc.priority}</span></td>
                <td>
                    <div class="btn-group btn-group-sm">
                        <button type="button" class="btn btn-info view-btn" data-id="${doc.id}">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button type="button" class="btn btn-primary edit-btn" data-id="${doc.id}">
                            <i class="fas fa-edit"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
        
        $tbody.html(rows);
    }
    
    function getStatusBadgeClass(status) {
        switch (status) {
            case 'New': return 'primary';
            case 'InProgress': return 'info';
            case 'Completed': return 'success';
            default: return 'secondary';
        }
    }
    
    // Public interface
    return {
        init: function() {
            this.bindEvents();
            this.loadDocuments();
        },
        
        bindEvents: function() {
            $('#searchBtn').on('click', function() {
                currentPage = 1;
                currentFilters.searchTerm = $('#searchTerm').val();
                currentFilters.status = $('#statusFilter').val();
                documentManagement.loadDocuments();
            });
            
            $('#documentsTable').on('click', '.view-btn', function() {
                const id = $(this).data('id');
                documentManagement.openDetailsView(id);
            });
            
            $('#documentsTable').on('click', '.edit-btn', function() {
                const id = $(this).data('id');
                documentManagement.openEditForm(id);
            });
        },
        
        loadDocuments: function() {
            $.ajax({
                url: '/DocumentAdmin/List',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    page: currentPage,
                    pageSize: pageSize,
                    searchTerm: currentFilters.searchTerm,
                    status: currentFilters.status
                }),
                success: function(response) {
                    if (response.success) {
                        renderTable(response.data.items);
                        totalPages = response.data.totalPages;
                    } else {
                        toastr.error(response.message || 'Failed to load documents');
                    }
                },
                error: function() {
                    toastr.error('An error occurred while loading documents');
                }
            });
        },
        
        openDetailsView: function(id) {
            $.ajax({
                url: `/DocumentAdmin/Details/${id}`,
                type: 'GET',
                success: function(response) {
                    $('#sidePaneTitle').text('Document Details');
                    $('#sidePaneContent').html(response);
                    $('#sidePane').removeClass('d-none');
                }
            });
        },
        
        openEditForm: function(id) {
            $.ajax({
                url: `/DocumentAdmin/Edit/${id}`,
                type: 'GET',
                success: function(response) {
                    $('#sidePaneTitle').text('Edit Document');
                    $('#sidePaneContent').html(response);
                    $('#sidePane').removeClass('d-none');
                    
                    // Initialize form validation
                    $.validator.unobtrusive.parse('#editDocumentForm');
                }
            });
        }
    };
})();

// Initialize when document ready
$(document).ready(function() {
    documentManagement.init();
});
```

## Error Handling

### Global Exception Handling
Example:
```csharp
// API Program.cs - Global exception handler
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        
        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (contextFeature != null)
        {
            logger.LogError(contextFeature.Error, "Unhandled exception");
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while processing your request."
            }));
        }
    });
});

// Web Program.cs - Error page handling
app.UseExceptionHandler("/Home/Error");
```

### Custom Error Pages
Example:
```csharp
// HomeController - Error handling
[Route("Error")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public IActionResult Error()
{
    return View(new ErrorViewModel 
    { 
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
    });
}
```

## ASP.NET Core Identity Integration

### Required NuGet Packages

Add these packages to your projects:
Example:
```bash
# Domain project
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# Infrastructure project  
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Identity

# API project
dotnet add package Microsoft.AspNetCore.Identity
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### Update DbContext for Identity
Example:
```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentHistory> DocumentHistory { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Apply configurations
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new DocumentConfiguration());
        builder.ApplyConfiguration(new DocumentHistoryConfiguration());
    }
}
```

### Identity Service Implementation
Example:
```csharp
public class IdentityAuthService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    
    public IdentityAuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }
    
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null || !user.IsActive)
            return Result<LoginResponseDto>.Failure("Invalid username or password");
            
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Result<LoginResponseDto>.Failure("Invalid username or password");
            
        var token = await GenerateJwtTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        
        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        var loginResponse = new LoginResponseDto
        {
            Id = user.Id,
            Username = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Token = token,
            Roles = roles.ToList(),
            DepartmentId = user.DepartmentId
        };
        
        return Result<LoginResponseDto>.Success(loginResponse);
    }
    
    public async Task<Result<bool>> RegisterAsync(RegisterRequestDto request)
    {
        var existingUser = await _userManager.FindByNameAsync(request.Username);
        if (existingUser != null)
            return Result<bool>.Failure("Username already exists");
            
        var existingEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
            return Result<bool>.Failure("Email already exists");
            
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };
        
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<bool>.Failure($"Failed to create user: {errors}");
        }
        
        // Assign role
        if (!string.IsNullOrEmpty(request.Role))
            await _userManager.AddToRoleAsync(user, request.Role);
            
        return Result<bool>.Success(true, "User created successfully");
    }
    
    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("FullName", user.FullName)
        };
        
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
            
        if (!string.IsNullOrEmpty(user.DepartmentId))
            claims.Add(new Claim("DepartmentId", user.DepartmentId));
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Identity DTOs
Example:
```csharp
public class RegisterRequestDto
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; }
    
    [Required(ErrorMessage = "Full name is required")]
    public string FullName { get; set; }
    
    public string DepartmentId { get; set; }
    public string Role { get; set; } = "StandardUser";
}

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; }
    
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; }
    
    [Required(ErrorMessage = "Please confirm your new password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; }
}
```

### Program.cs Configuration
Example:
```csharp
// API Program.cs
builder.Services.AddInfrastructure(builder.Configuration); // Includes Identity setup

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"] ?? "dev_super_secret_key"))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("StandardUser", "Admin"));
});

// Web Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});
```

### Database Migration for Identity
Example:
```bash
# Create Identity migration
dotnet ef migrations add AddIdentity --project EnterpriseApp.Infrastructure --startup-project EnterpriseApp.Api

# Update database
dotnet ef database update --project EnterpriseApp.Infrastructure --startup-project EnterpriseApp.Api
```

### Seed Initial Data
Example:
```csharp
// Add to API Program.cs after app.UseAuthorization()
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // Create roles
    await EnsureRoleAsync(roleManager, "Admin");
    await EnsureRoleAsync(roleManager, "ManagerUser");
    await EnsureRoleAsync(roleManager, "StandardUser");
    
    // Create admin user
    await EnsureAdminUserAsync(userManager);
}

async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
{
    if (!await roleManager.RoleExistsAsync(roleName))
        await roleManager.CreateAsync(new IdentityRole(roleName));
}

async Task EnsureAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    var adminEmail = "admin@enterprise.com";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    
    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };
        
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
```

### Standard HttpService Implementation

Use this HttpService consistently across all projects:
Example:
```csharp
public class HttpService : IHttpService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<HttpService> _logger;
    
    public HttpService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<HttpService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
    
    private async Task<string?> GetTokenAsync()
    {
        if (_httpContextAccessor.HttpContext == null) return null;

        var result = await _httpContextAccessor.HttpContext.AuthenticateAsync();
        if (!result.Succeeded) return null;

        var tokenClaim = result.Principal?.Claims.FirstOrDefault(c => c.Type == "access_token");
        return tokenClaim?.Value;
    }
    
    private async Task<HttpRequestMessage> CreateRequest(HttpMethod method, string url, object? data = null)
    {
        var request = new HttpRequestMessage(method, url);

        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
        {
            var json = JsonSerializer.Serialize(data);
            var jsonContent = new StringContent(json, Encoding.UTF8);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = jsonContent;
        }

        return request;
    }
    
    private async Task<TResponse> SendRequestAsync<TResponse>(HttpMethod method, string url, object? data = null)
    {
        try
        {
            using var request = await CreateRequest(method, url, data);
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                throw new UnauthorizedAccessException("Session expired. Please login again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request failed with status {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions)!;
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not HttpRequestException)
        {
            _logger.LogError(ex, "Error during {Method} request to {Url}", method, url);
            throw;
        }
    }
    
    // Public methods
    public async Task<TResponse> GetAsync<TResponse>(string url) => 
        await SendRequestAsync<TResponse>(HttpMethod.Get, url);
        
    public async Task<TResponse> PostAsync<TResponse>(string url, object data) => 
        await SendRequestAsync<TResponse>(HttpMethod.Post, url, data);
        
    public async Task<TResponse> PutAsync<TResponse>(string url, object data) => 
        await SendRequestAsync<TResponse>(HttpMethod.Put, url, data);
        
    public async Task<TResponse> DeleteAsync<TResponse>(string url) => 
        await SendRequestAsync<TResponse>(HttpMethod.Delete, url);
        
    // File upload methods
    public async Task<TResponse> PostWithFileAsync<TResponse>(string url, IFormFile file, Dictionary<string, string>? additionalData = null)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);

        if (additionalData != null)
        {
            foreach (var item in additionalData)
                content.Add(new StringContent(item.Value), item.Key);
        }

        using var request = await CreateFileRequest(HttpMethod.Post, url, content);
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"File upload failed: {response.StatusCode}");
            
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions)!;
    }
    
    private async Task<HttpRequestMessage> CreateFileRequest(HttpMethod method, string url, MultipartFormDataContent content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
        return request;
    }
}
```

## Deployment

### Development Environment
- Local development using IIS Express or Kestrel
- SQL Server LocalDB or SQL Server Express for database
- Visual Studio 2022 for development

### Production Environment
- **Web Hosting**: Azure App Service or IIS
- **Database**: Azure SQL Database or SQL Server
- **Security**: Azure Key Vault for secrets management
- **Monitoring**: Azure Application Insights or similar
- **CI/CD**: Azure DevOps or GitHub Actions

### Configuration Management
Example:
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SULOCHANNEW\\SQLEXPRESS;Database=EnterpriseAppDB;User Id=sa;Password=sa_123;TrustServerCertificate=true;"
  },
  "Jwt": {
    "Key": "your-super-secret-key-change-in-production",
    "Issuer": "EnterpriseApp.Api",
    "Audience": "EnterpriseApp.Web"
  },
  "ApiBaseUrl": "https://localhost:7001"
}
```
### Data Protection & Compliance
csharp
// Add data protection for GDPR/compliance
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("EnterpriseApp");

// Add audit logging service
public interface IAuditService
{
    Task LogAuditAsync(string userId, string action, string entityType, 
        string entityId, string oldValues, string newValues);
    Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(AuditFilterDto filter);
}
### Performance & Scalability

csharp
// Add caching strategy
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Add response compression
builder.Services.AddResponseCompression();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>()
    .AddSqlServer(connectionString);

### Logging & Monitoring

csharp
// Add comprehensive logging strategy
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.AddApplicationInsights(); // For production
});

// Add structured logging with Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
### Enhanced Error Handling

csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (BusinessException ex)
        {
            await HandleBusinessExceptionAsync(context, ex);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericExceptionAsync(context, ex);
        }
    }
}
###  Input Validation & Sanitization

csharp
public class DocumentRequestDto
{
    [Required]
    [StringLength(200)]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_\.]+$")] // Prevent injection
    public string Title { get; set; }
    
    [AllowHtml(false)] // Custom attribute to prevent HTML
    public string Description { get; set; }
}
### Rate Limiting

csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
    });
});
## Best Practices Summary

1. **Architecture**: Follow Clean Architecture principles with clear separation of concerns
2. **No Repository Pattern**: Use services directly for data access
3. **No MediatR**: Use direct service injection instead
4. **No AutoMapper**: Use manual mapping for better control
5. **DTOs Only**: Use DTOs from `.Application` or `.Shared`, no ViewModels in Web project
6. **External Assets**: All CSS/JS in external files, no inline styles or scripts
7. **Client-Side**: Leverage jQuery Ajax and partial views for dynamic content
8. **Security**: Implement JWT for API, cookies for Web, with proper CSRF protection
9. **Validation**: Use Data Annotations with client-side jQuery validation
10. **Error Handling**: Global exception handling with consistent error responses
11. **HttpService**: Centralized service for all API communication from Web to API
12. **Identity**: Use ASP.NET Core Identity for authentication and user management

This framework ensures consistent, secure, and maintainable enterprise application development following modern best practices and proven architectural patterns.