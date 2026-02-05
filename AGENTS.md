# AGENTS.md - Development Guidelines

This is a full-stack application with **Frontend**: Astro 5.16.11 + React + TypeScript + Tailwind CSS v4, **Backend**: .NET 10.0 Web API + MongoDB, **Package Manager**: Bun (frontend), dotnet (backend), **Containerization**: Docker.

## Build Commands

### Frontend (in `/frontend`)

```bash
bun run dev              # Start dev server at localhost:4321
bun run build            # Build for production
bun run preview          # Preview production build
bun run check            # Run Astro type checking
bun run gen:api          # Generate TypeScript API client from Swagger
```

### Backend (in `/backend`)

```bash
dotnet run               # Start development server (localhost:5042)
dotnet build             # Build the project
dotnet test              # Run all tests
dotnet test --filter "TestMethodName"  # Run specific test
dotnet publish -c Release    # Build for production
```

### Docker Commands (root)

```bash
docker-compose up       # Start all services (frontend, backend, mongodb)
docker-compose down     # Stop all services
docker-compose build    # Rebuild containers
```

## Code Style Guidelines

### TypeScript/React

**Imports**: Use `@/*` path aliases for internal imports:

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

**Styling**: Tailwind CSS v4 with shadcn/ui (New York style), utility-first approach, mobile-first breakpoints.

### C#

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

**Conventions**: PascalCase for classes/interfaces, camelCase for parameters, `I` prefix for interfaces (e.g., `IProductRepository`), async suffix for async methods (e.g., `GetByIdAsync`).

## File Organization

```
frontend/src/
├── components/     # React/Astro components
├── pages/          # Astro pages
├── lib/            # Utilities, API clients
├── layouts/        # Astro layouts
└── actions/        # Server actions

backend/
├── Controllers/    # API controllers
├── Models/         # Data models
├── Repositories/   # Data access layer
├── DTOs/           # Data transfer objects
└── Enums/          # Enumerations
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

## Git Commit Standards

Follow conventional commits from `.github/instructions/commit-messages.intructions.md`:

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
- `/frontend/tsconfig.json` - TypeScript strict mode
- `/frontend/components.json` - shadcn/ui configuration
- `/backend/backend.csproj` - .NET project with XML docs enabled
- `/docker-compose.yml` - Docker services

## Security Notes

- Never commit secrets or API keys
- Use environment variables for configuration
- Validate all inputs in both frontend and backend
- Follow .NET security best practices
