# Plan: Create AGENTS.md File

## Analysis Summary

I've conducted a thorough analysis of the codebase and gathered all necessary information to create a comprehensive AGENTS.md file. Here's what I discovered:

### Project Structure
- **Frontend**: Astro 5.16.11 + React + TypeScript + Tailwind CSS v4 (uses Bun)
- **Backend**: .NET 10.0 Web API + MongoDB + Entity Framework Core
- **Containerization**: Docker with docker-compose
- **No existing AGENTS.md, cursor rules, or copilot instructions found**

### Key Findings

1. **Build Commands Available**:
   - Frontend: `bun run dev`, `bun run build`, `bun run preview`, `bun run check`, `bun run gen:api`
   - Backend: `dotnet run`, `dotnet build`, `dotnet test` (not implemented)
   - Docker: `docker-compose up/down/build`

2. **Testing Status**: 
   - No test framework currently configured
   - Need to note this and provide recommendations

3. **Linting/Formatting**:
   - No explicit linting configuration found
   - Provide recommended setup in the guidelines

4. **Code Style Patterns**:
   - Frontend: TypeScript strict mode, shadcn/ui components, `cn()` utility, Tailwind CSS
   - Backend: C# with XML documentation, repository pattern, DTOs, async/await
   - Git: Existing commit message standards in `.github/instructions/commit-messages.intructions.md`

5. **Key Configuration Files**:
   - `/frontend/astro.config.mjs`, `/frontend/tsconfig.json`, `/frontend/components.json`
   - `/backend/backend.csproj` (with XML docs enabled)
   - `/docker-compose.yml`

## Implementation Plan

I will create an AGENTS.md file approximately 150 lines long with the following sections:

### 1. Project Overview (5 lines)
- Brief description of tech stack and architecture

### 2. Build Commands (15 lines)
- Frontend commands with explanations
- Backend commands with explanations  
- Docker commands
- Note about testing status

### 3. Code Style Guidelines (80 lines)
- **Frontend (TypeScript/Astro)**:
  - File structure & imports with examples
  - Component conventions
  - TypeScript patterns
  - Styling conventions
  
- **Backend (C#)**:
  - Controller pattern with XML documentation
  - C# conventions
  - Error handling patterns

### 4. Development Environment (10 lines)
- Prerequisites
- Environment variables

### 5. API Development (10 lines)
- Backend API standards
- Frontend integration patterns

### 6. File Organization (10 lines)
- Frontend structure
- Backend structure

### 7. Git Commit Standards (10 lines)
- Reference to existing commit message instructions
- Key requirements summary

### 8. Linting & Formatting (10 lines)
- Current status (none configured)
- Recommended setup

### 9. Key Configuration Files (5 lines)
- List important config files and their purposes

### 10. Common Patterns (15 lines)
- Frontend component pattern example
- Backend repository pattern example

### 11. Development Workflow (5 lines)
- Step-by-step development process

### 12. Security Notes (5 lines)
- Important security practices

## Content Highlights

The AGENTS.md will include:
- Specific commands for running single tests (when implemented)
- Code examples from actual codebase (ProductsController, utils.ts)
- Integration with existing commit message standards
- Practical examples for both frontend and backend patterns
- Docker workflow for full-stack development
- API client generation instructions

## File Location
Will create `/home/anass/workspaces/mi-cuatri/AGENTS.md` in the repository root.

This plan ensures comprehensive coverage of all aspects agentic coding agents need to work effectively in this codebase, with specific examples and practical guidance.