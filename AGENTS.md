# AGENTS.md - Development Guidelines for Agentic Coding

This file contains build commands, code style guidelines, and development conventions for agentic coding agents working in this repository.

## Project Overview

This is a full-stack application with:
- **Frontend**: Astro 5.16.11 + React + TypeScript + Tailwind CSS v4
- **Backend**: .NET 10.0 Web API + MongoDB + Entity Framework Core
- **Package Manager**: Bun (frontend), dotnet (backend)
- **Containerization**: Docker with docker-compose

## Build Commands

### Frontend (in `/frontend`)
```bash
# Development
bun run dev              # Start dev server at localhost:4321
bun run build            # Build for production
bun run preview          # Preview production build
bun run check            # Run Astro type checking
bun run astro            # Run Astro CLI commands

# API Generation
bun run gen:api          # Generate TypeScript API client from Swagger
```

### Backend (in `/backend`)
```bash
# Development
dotnet run               # Start development server (localhost:5042)
dotnet build             # Build the project
dotnet test              # Run tests (when implemented)

# Production
dotnet publish -c Release    # Build for production deployment
```

### Docker Commands (root)
```bash
docker-compose up       # Start all services (frontend, backend, mongodb)
docker-compose down     # Stop all services
docker-compose build    # Rebuild containers
```

## Testing

### Current Status
- No test framework currently configured
- Recommended: Add Vitest + Testing Library for frontend, xUnit for backend

### Running Tests (when implemented)
```bash
# Frontend
bun run test            # Run all tests
bun run test path/to/file.test.ts  # Run single test file

# Backend  
dotnet test             # Run all tests
dotnet test --filter "TestMethodName"  # Run specific test
```

## Code Style Guidelines

### Frontend (TypeScript/Astro)

#### File Structure & Imports
```typescript
// Use @/* path aliases for internal imports
import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
```

#### Component Conventions
- Use PascalCase for component files and names
- Export components as default
- Use shadcn/ui component pattern with `cn()` utility
- All styling via Tailwind CSS classes

#### TypeScript Patterns
```typescript
// Strict typing with interfaces/types
interface ProductDto {
  id: string
  name: string
  price: number
  quantity: number
}

// Async functions with proper error handling
export async function getProducts(): Promise<ProductDto[]> {
  try {
    const response = await fetch('/api/products')
    if (!response.ok) throw new Error('Failed to fetch')
    return response.json()
  } catch (error) {
    console.error('Error fetching products:', error)
    throw error
  }
}
```

#### Styling Conventions
- Use Tailwind CSS v4 with shadcn/ui (New York style)
- Utility-first approach
- Responsive design with mobile-first breakpoints
- Use `cn()` helper for conditional classes

### Backend (C#)

#### Controller Pattern
```csharp
/// <summary>
/// Controller for managing product operations.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    
    /// <summary>
    /// Initializes a new instance of the ProductsController class.
    /// </summary>
    /// <param name="productRepository">The product repository for data access.</param>
    public ProductsController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
}
```

#### C# Conventions
- XML documentation for all public APIs
- Repository pattern with dependency injection
- DTO pattern for data transfer
- Async/await for all database operations
- Proper HTTP status codes and response types
- Nullable reference types enabled

#### Error Handling
```csharp
[HttpGet("{id}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ProductDto>> GetById(string id)
{
    var product = await _productRepository.GetProductByIdAsync(id);
    if (product == null) return NotFound();
    
    var productDto = new ProductDto(product.Id, product.Name, product.Price, product.Quantity);
    return Ok(productDto);
}
```

## Development Environment

### Prerequisites
- Bun (frontend package manager)
- .NET 10.0 SDK
- Docker & Docker Compose
- MongoDB (handled via Docker)

### Environment Variables
```bash
# Backend (.env)
ConnectionStrings__MongoDB=mongodb://user:user@localhost:27017/mi-cuatri?authSource=admin
ASPNETCORE_ENVIRONMENT=Development

# Frontend (.env)  
INTERNAL_API_BASE_URL=http://localhost:5042
```

## API Development

### Backend API Standards
- Use Swagger/OpenAPI with XML documentation
- Access at `http://localhost:5042/swagger`
- Auto-generate TypeScript client with `bun run gen:api`
- Follow RESTful conventions

### Frontend API Integration
```typescript
// Use generated API client from src/lib/api.ts
import { api } from '@/lib/api'

export async function getProducts() {
  return await api.products.getAll()
}
```

## File Organization

### Frontend Structure
```
frontend/src/
├── components/     # React/Astro components
├── pages/         # Astro pages  
├── lib/           # Utilities, API clients, helpers
├── layouts/       # Astro layouts
├── actions/       # Server actions
└── styles/        # Global styles
```

### Backend Structure
```
backend/
├── Controllers/   # API controllers
├── Models/        # Data models
├── Repositories/  # Data access layer
├── Services/      # Business logic
├── DTOs/          # Data transfer objects
└── Enums/         # Enumerations
```

## Git Commit Standards

Follow the conventional commit format from `.github/instructions/commit-messages.intructions.md`:

```
type(scope): message

Detailed description of changes made, including context,
reason for changes, and any important implementation details.

Files modified:
- path/to/file1.ext
- path/to/file2.ext
```

- **Types**: feat, fix, docs, style, refactor, perf, test, chore
- **Scopes**: core, operations, shared  
- **Message**: imperative mood, lowercase, no period, max 48 chars
- **Language**: English only

## Linting & Formatting

### Current Status
No explicit linting configuration currently set up.

### Recommended Setup
Add to `frontend/package.json`:
```json
{
  "devDependencies": {
    "eslint": "^8.0.0",
    "prettier": "^3.0.0", 
    "@typescript-eslint/eslint-plugin": "^6.0.0",
    "prettier-plugin-tailwindcss": "^0.5.0"
  },
  "scripts": {
    "lint": "eslint . --ext ts,tsx,astro",
    "lint:fix": "eslint . --ext ts,tsx,astro --fix",
    "format": "prettier --write .",
    "format:check": "prettier --check ."
  }
}
```

## Key Configuration Files

- `/frontend/astro.config.mjs` - Astro configuration
- `/frontend/tsconfig.json` - TypeScript strict mode
- `/frontend/components.json` - shadcn/ui configuration  
- `/backend/backend.csproj` - .NET project with XML docs enabled
- `/docker-compose.yml` - Docker services

## Common Patterns

### Frontend Component Pattern
```typescript
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"

interface ComponentProps {
  className?: string
  children: React.ReactNode
}

export function Component({ className, children }: ComponentProps) {
  return (
    <div className={cn("default-classes", className)}>
      {children}
    </div>
  )
}
```

### Backend Repository Pattern
```csharp
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(string id);
    Task AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(string id);
}
```

## Development Workflow

1. Start services: `docker-compose up`
2. Frontend dev: `bun run dev` (localhost:4321)
3. Backend dev: `dotnet run` (localhost:5042) 
4. API docs: http://localhost:5042/swagger
5. Generate API client: `bun run gen:api`

## Security Notes

- Never commit secrets or API keys
- Use environment variables for configuration
- Follow .NET security best practices
- Validate all inputs in both frontend and backend