# AGENTS.md - Development Guidelines

This is a full-stack application with **Frontend**: Astro 5.16.11 + React + TypeScript + Tailwind CSS v4, **Backend**: .NET 10.0 Web API + MongoDB, **Package Manager**: Bun (frontend), dotnet (backend), **Containerization**: Docker.

## Build, Lint & Test Commands

### Frontend (in `/frontend`)

**Development & Building:**

```bash
bun run dev              # Start dev server at localhost:4321
bun run build            # Build for production
bun run preview          # Preview production build
bun run check            # Check Astro types on all files
bun run gen:api          # Generate TypeScript API client from Swagger
bun test                 # Run all tests
```

### Backend (in `/backend`)

**Development & Building:**

```bash
dotnet run                   # Start development server (localhost:5042)
dotnet build                 # Build the project
dotnet publish -c Release    # Build for production
```

**Testing (in `/backend.Tests`):**

```bash
cd backend.Tests && dotnet run                                            # Run all tests
cd backend.Tests && dotnet run -- -class Namespace.ClassName              # Run tests in specific class
cd backend.Tests && dotnet test -- -method Namespace.ClassName.MethodName # Run specific test method
cd backend.Tests && dotnet run -- -list full                              # List all discovered tests
```

### Docker Commands (root)

```bash
docker-compose up       # Start all services (frontend, backend, mongodb)
docker-compose down     # Stop all services
docker-compose build    # Rebuild containers
docker-compose logs -f  # View live logs from all services
```

### E2E Testing (in `/tests-e2e`)

**Playwright E2E Tests:**

```bash
bun install              # Install Playwright dependencies (first time)
bun run test             # Run all E2E tests
bun run test:ui          # Run tests with interactive UI mode
bun run test:debug       # Run tests in debug mode
```

**Test Files:**
- `tests/auth.spec.ts` - Authentication tests
- `tests/calendar.spec.ts` - Calendar functionality tests
- `tests/profile.spec.ts` - Profile management tests
- `tests/seed.spec.ts` - Data seeding tests
- `tests/fixtures.ts` - Shared test fixtures and helpers

## Code Style Guidelines

### TypeScript/React

**Imports**: Use `@/*` path aliases for internal imports. Separate external and internal imports:

```typescript
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
```

**Components**: Use PascalCase for component files and names, export as default, use `cn()` utility for conditional classes:

```typescript
interface ComponentProps {
  className?: string
  children: React.ReactNode
}

export default function Component({ className, children }: ComponentProps) {
  return <div className={cn("default-classes", className)}>{children}</div>
}
```

**Astro-Specific Patterns**:

- Server components run only at build time; use for API calls, database queries, file access
- Server-rendered HTML hydrated with React client components using `client:load`, `client:idle`, etc.

**Types**: Strict TypeScript with interfaces/types, use records for DTOs, nullable reference types enabled:

```typescript
interface ProductDto {
  id: string;
  name: string;
  price: number;
  quantity: number;
}

// Async functions with proper error handling
export async function getProducts(): Promise<ProductDto[]> {
  try {
    const response = await fetch('/api/products');
    if (!response.ok) throw new Error('Failed to fetch');
    return response.json();
  } catch (error) {
    console.error('Error fetching products:', error);
    throw error;
  }
}
```

**Styling**: Tailwind CSS v4 with shadcn/ui (New York style), utility-first approach, mobile-first breakpoints. Use `cn()` from `@/lib/utils` to merge Tailwind classes safely.

### C #

**Controllers**: XML documentation for all public APIs, repository pattern with DI, proper HTTP status codes:

```csharp
/// <summary>Controller for managing product operations.</summary>
[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;

    public ProductsController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>Gets a product by its ID.</summary>
    /// <param name="id">The product ID.</param>
    /// <returns>The product details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(string id)
    {
        var product = await _productRepository.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(new ProductDto(product.Id, product.Name, product.Price, product.Quantity));
    }
}
```

**Records for DTOs**:

```csharp
public record ProductDto(string Id, string Name, double Price, int Quantity);
```

**Error Handling**: Use try-catch with meaningful error messages, log exceptions, return appropriate HTTP status codes:

```csharp
try
{
    var result = await _productRepository.GetProductByIdAsync(id);
    if (result == null) return NotFound("Product not found");
    return Ok(result);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error fetching product {ProductId}", id);
    return StatusCode(500, "An error occurred while processing your request");
}
```

**Conventions**:

- PascalCase for classes/interfaces/methods
- camelCase for parameters and local variables
- `I` prefix for interfaces (e.g., `IProductRepository`)
- `Async` suffix for async methods (e.g., `GetByIdAsync`)
- `_` prefix for private fields (e.g., `_productRepository`)
- XML comments (`///`) for all public members

**Testing Conventions** (xUnit + FluentAssertions):

- Use `[Fact]` for single test cases and `[Theory]` for parameterized tests
- Test method naming: `MethodName_Scenario_ExpectedResult` (e.g., `GetById_WithValidId_ReturnsProduct`)
- Use FluentAssertions for readable assertions (`.Should().Be()`, `.Should().NotBeNull()`, etc.)
- Organize tests in `/backend.Tests` with folder structure matching backend source (`Unit/Controllers`, `Unit/Services`, `Unit/Enums`, etc.)
- Use `#region` to organize test sections (Arrange, Act, Assert or by method)
- Constructor initialization for test fixtures and System Under Test (SUT)

## File Organization

```
frontend/
├── src/
│   ├── components/     # React/Astro components
│   ├── pages/          # Astro pages
│   ├── lib/            # Utilities, API clients
│   ├── layouts/        # Astro layouts
│   └── actions/        # Server actions
└── tests/              # Unit tests (reducers, types, utilities, etc.)

backend/
├── Controllers/    # API controllers
├── Models/         # Data models
├── Repositories/   # Data access layer
├── DTOs/           # Data transfer objects
└── Enums/          # Enumerations

backend.Tests/
├── Unit/           # Unit tests (Controllers, Repositories, Services, Enums)
├── Integration/    # Integration tests
└── Helpers/        # Test utilities and fixtures
```

## Development Environment

**Prerequisites**: Bun, .NET 10.0 SDK, Docker, MongoDB (via Docker)

**Environment Variables**:

```bash
# Backend (.env)
ConnectionStrings__MongoDB=mongodb://user:user@localhost:27017/mi-cuatri?authSource=admin
ASPNETCORE_ENVIRONMENT=Development

# Frontend (.env)
INTERNAL_API_BASE_URL=http://localhost:5042
```

**API Access**: Swagger docs at `http://localhost:5042/swagger`

## Commit Standards

Follow conventional commits from `.github/instructions/commit-messages.instructions.md`:

```
type(scope): message

Detailed description of changes made, including context and reasoning.

Files modified:
- path/to/file1.ext
- path/to/file2.ext
```

- **Types**: feat, fix, docs, style, refactor, perf, test, chore
- **Scopes**: core, operations, shared
- **Message**: imperative mood, lowercase, no period, max 48 chars
- **Language**: English only

## Key Configuration Files

- `/frontend/astro.config.mjs` - Astro configuration
- `/frontend/tsconfig.json` - TypeScript strict mode with path aliases (`@/*`)
- `/frontend/components.json` - shadcn/ui configuration
- `/backend/backend.csproj` - .NET project with XML docs enabled, nullable reference types
- `/docker-compose.yml` - Docker services (backend, frontend, MongoDB, mongo-express)

## Security Notes

- Never commit secrets or API keys
- Use environment variables for configuration
- Validate all inputs in both frontend and backend
- Follow .NET security best practices
