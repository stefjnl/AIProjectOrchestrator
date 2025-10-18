# Configuration Guide for AIProjectOrchestrator

## 🔐 Secure Configuration Setup

This guide explains how to configure API keys and sensitive settings securely without committing them to source control.

## ⚠️ Security Warning

**NEVER commit API keys or sensitive credentials to source control!**

All API keys have been removed from `appsettings.json`. You must configure them using one of the secure methods below.

---

## Method 1: User Secrets (Development - Recommended)

For local development, use .NET User Secrets to store sensitive configuration:

### Setup Commands

```powershell
# Navigate to the API project directory
cd src\AIProjectOrchestrator.API

# Initialize user secrets (already done if UserSecretsId exists in .csproj)
dotnet user-secrets init

# Set OpenRouter API Key
dotnet user-secrets set "AIProviders:Providers:OpenRouter:ApiKey" "your-openrouter-api-key-here"

# Set Claude API Key (if using Claude)
dotnet user-secrets set "AIProviders:Providers:Claude:ApiKey" "your-claude-api-key-here"

# Set legacy configuration (for backward compatibility)
dotnet user-secrets set "AIProviderConfigurations:OpenRouter:ApiKey" "your-openrouter-api-key-here"
dotnet user-secrets set "AIProviderConfigurations:Claude:ApiKey" "your-claude-api-key-here"

# Set database password if needed
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=aiprojectorchestrator;Username=user;Password=your-password"
```

### View Current Secrets

```powershell
dotnet user-secrets list
```

### Remove a Secret

```powershell
dotnet user-secrets remove "AIProviders:Providers:OpenRouter:ApiKey"
```

### Clear All Secrets

```powershell
dotnet user-secrets clear
```

---

## Method 2: Environment Variables (Production)

For production deployment, use environment variables:

### Windows PowerShell

```powershell
$env:AIProviders__Providers__OpenRouter__ApiKey = "your-api-key"
$env:AIProviders__Providers__Claude__ApiKey = "your-api-key"
```

### Linux/macOS

```bash
export AIProviders__Providers__OpenRouter__ApiKey="your-api-key"
export AIProviders__Providers__Claude__ApiKey="your-api-key"
```

### Docker

In `docker-compose.yml`:

```yaml
services:
  api:
    environment:
      - AIProviders__Providers__OpenRouter__ApiKey=your-api-key
      - AIProviders__Providers__Claude__ApiKey=your-api-key
```

Or use a `.env` file (add to `.gitignore`):

```env
AIProviders__Providers__OpenRouter__ApiKey=your-api-key
AIProviders__Providers__Claude__ApiKey=your-api-key
```

---

## Method 3: Azure Key Vault (Production - Recommended)

For Azure deployments, use Azure Key Vault:

### 1. Install NuGet Package

```powershell
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

### 2. Add to Program.cs

```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    
    builder.Configuration.AddAzureKeyVault(
        keyVaultUri,
        new DefaultAzureCredential());
}
```

### 3. Store Secrets in Key Vault

```powershell
# Using Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "AIProviders--Providers--OpenRouter--ApiKey" --value "your-api-key"
```

---

## Method 4: appsettings.Development.Local.json (Development - Not Recommended)

Create a local file that's excluded from source control:

### 1. Create File

Create `src/AIProjectOrchestrator.API/appsettings.Development.Local.json`:

```json
{
  "AIProviders": {
    "Providers": {
      "OpenRouter": {
        "ApiKey": "your-openrouter-api-key-here"
      },
      "Claude": {
        "ApiKey": "your-claude-api-key-here"
      }
    }
  }
}
```

### 2. Add to .gitignore

Ensure your `.gitignore` includes:

```gitignore
appsettings.Development.Local.json
appsettings.*.Local.json
```

---

## Configuration Hierarchy

.NET configuration sources are loaded in this order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments
6. Azure Key Vault (if configured)

---

## Required API Keys

### OpenRouter (Primary Provider)

- **Get Key:** https://openrouter.ai/keys
- **Configuration Path:** `AIProviders:Providers:OpenRouter:ApiKey`
- **Used For:** Most AI operations (Requirements, Planning, Stories, etc.)

### Claude (Optional)

- **Get Key:** https://console.anthropic.com/
- **Configuration Path:** `AIProviders:Providers:Claude:ApiKey`
- **Used For:** Alternative AI provider

### NanoGpt (Local/Proxy)

- **Configuration Path:** `AIProviders:Providers:NanoGpt:ApiKey`
- **Note:** Uses local proxy, API key may not be required

---

## Verification

After configuration, verify your setup:

### 1. Check Configuration Loading

Run the application and check logs for:

```
OpenRouter Settings - BaseUrl: https://openrouter.ai/api/v1, ApiKey Length: [should be > 0]
```

### 2. Test API Endpoint

```powershell
curl http://localhost:5000/api/health
```

### 3. Check Available Providers

```powershell
curl http://localhost:5000/api/providermanagement/available
```

---

## Troubleshooting

### API Key Not Found

**Symptom:** `ApiKey Length: 0` in logs

**Solutions:**
- Verify user secrets are set: `dotnet user-secrets list`
- Check environment variable names use double underscores (`__`)
- Ensure you're running from the correct project directory
- Restart the application after setting secrets

### Configuration Not Loading

**Symptom:** Still using empty API key

**Solutions:**
- Check configuration section names match exactly (case-sensitive)
- Verify `.csproj` has `<UserSecretsId>` element
- Clear and rebuild: `dotnet clean && dotnet build`

### Multiple Configuration Sources

If you have API keys in multiple sources, the last one loaded wins. Check all sources:

```powershell
# Check user secrets
dotnet user-secrets list

# Check environment variables
Get-ChildItem env: | Where-Object { $_.Name -like "*AIProviders*" }
```

---

## Security Best Practices

1. ✅ **DO** use User Secrets for development
2. ✅ **DO** use Azure Key Vault or similar for production
3. ✅ **DO** add sensitive files to `.gitignore`
4. ✅ **DO** rotate API keys regularly
5. ✅ **DO** use different keys for dev/staging/prod

6. ❌ **DON'T** commit API keys to source control
7. ❌ **DON'T** share API keys in chat/email
8. ❌ **DON'T** log API keys (even partially)
9. ❌ **DON'T** use production keys in development

---

## Quick Start (TL;DR)

For local development:

```powershell
cd src\AIProjectOrchestrator.API
dotnet user-secrets set "AIProviders:Providers:OpenRouter:ApiKey" "your-key-here"
dotnet run
```

Done! Your API keys are now secure and not in source control.
