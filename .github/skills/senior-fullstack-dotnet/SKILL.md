---
name: senior-fullstack-dotnet
description: 'Build end-to-end .NET fullstack solutions integrating backend APIs with frontend frameworks. Use for: .NET Core REST API + React or Angular, JWT authentication backend and frontend, API consumption with fetch/axios/HttpClient, CORS configuration, frontend interceptors, role-based authorization, SignalR real-time, Blazor, form validation, API contracts, TypeScript interfaces from C# models.'
argument-hint: 'Describe the fullstack feature or integration task (e.g. "JWT auth in .NET backend + Angular frontend")'
---

# Senior Fullstack .NET Skill

## When to Use
- Creating a .NET Core REST or Minimal API consumed by React or Angular
- Implementing JWT authentication end-to-end (backend issue + frontend storage/interceptor)
- Setting up CORS in ASP.NET Core for SPA frontends
- Generating TypeScript interfaces that mirror C# DTOs
- Adding real-time features with SignalR
- Role-based authorization across .NET and Angular route guards
- Scaffolding a fullstack project with both backend and frontend in one repo

## Procedure

### 1. Define the API Contract First
- Design DTOs (C# records) before writing controllers
- Generate or mirror TypeScript interfaces to match

```csharp
// Backend DTO
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt);
```

```typescript
// Frontend mirror (TypeScript)
export interface LoginRequest { email: string; password: string; }
export interface LoginResponse { token: string; expiresAt: string; }
```

### 2. Backend Setup (.NET Core / Minimal API)

**JWT Authentication**
```csharp
// Program.cs - register JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// CORS for SPA
builder.Services.AddCors(options =>
    options.AddPolicy("SpaPolicy", policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

app.UseCors("SpaPolicy");
app.UseAuthentication();
app.UseAuthorization();
```

### 3. Frontend Integration

**Angular HTTP Interceptor (JWT)**
```typescript
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.authService.getToken();
    if (token) {
      req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }
    return next.handle(req);
  }
}
```

**React fetch with token**
```typescript
export async function apiFetch<T>(url: string, options?: RequestInit): Promise<T> {
  const token = localStorage.getItem('token');
  const res = await fetch(`${import.meta.env.VITE_API_URL}${url}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options?.headers },
  });
  if (!res.ok) throw new Error(await res.text());
  return res.json() as Promise<T>;
}
```

### 4. Validate Integration
- Test CORS preflight with browser DevTools (Network → OPTIONS request)
- Confirm JWT claims reach the backend (`HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)`)
- Verify token expiry is handled (401 → redirect to login on frontend)
- Check that environment variables (`VITE_API_URL`, `appsettings.json` Jwt section) are configured for each environment
