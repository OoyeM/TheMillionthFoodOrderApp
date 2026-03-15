# Clean Architecture

## Purpose
Project structure and dependency flow: Api -> Application -> Domain -> Infrastructure.

## Patterns

### Adding a New Feature (Vertical Slice)
Follow this order when adding a new bounded context or entity:

1. **Domain** — Aggregate root with factory methods, repository interface
   - `TheMillionthFoodOrderApp.Domain/{Feature}/{Feature}.cs` — entity
   - `TheMillionthFoodOrderApp.Domain/{Feature}/I{Feature}Repository.cs` — interface

2. **Infrastructure** — EF Core config, repository impl, migrations
   - `TheMillionthFoodOrderApp.Infrastructure/{Feature}/{Feature}Configuration.cs` — EF config
   - `TheMillionthFoodOrderApp.Infrastructure/{Feature}/{Feature}Repository.cs` — repository impl
   - Register `DbSet<T>` in appropriate DbContext (Platform or Brand)
   - Generate migration

3. **Application** — Service interface, service impl, DTOs
   - `TheMillionthFoodOrderApp.Application/{Feature}/I{Feature}Service.cs` — service interface
   - `TheMillionthFoodOrderApp.Application/{Feature}/{Feature}Service.cs` — service impl
   - `TheMillionthFoodOrderApp.Application/{Feature}/{Feature}Dtos.cs` — request/response records

4. **Api** — FastEndpoints endpoint classes
   - `TheMillionthFoodOrderApp.Api/Endpoints/{Feature}/Get{Feature}Endpoint.cs`
   - `TheMillionthFoodOrderApp.Api/Endpoints/{Feature}/Update{Feature}Endpoint.cs`

5. **DI Registration** — Register in both DI files
   - `Infrastructure/DependencyInjection.cs` — repository
   - `Application/DependencyInjection.cs` — service

### Platform vs Brand Scoped
- **Platform entities** (Brand, PlatformUser, BrandUserRole) -> `PlatformDbContext`, endpoints at `/api/brands`, `/api/users`
- **Brand entities** (BrandSettings, future: Shop, Product, Order) -> `BrandDbContext`, endpoints at `/api/brands/{brandSlug}/...`
- Brand-scoped endpoints must add `PreProcessor<BrandScopedPreProcessor<TRequest>>()` in their `Configure()` method

## Gotchas

- Domain layer has **zero dependencies** on Infrastructure or Application — only pure C# and the `Common/` base classes
- Application layer defines **interfaces** that Infrastructure implements (Dependency Inversion)
- Never reference `DbContext` or EF Core from Domain or Application layers
- DTOs live in Application, not Domain — Domain entities use private setters and factory methods
