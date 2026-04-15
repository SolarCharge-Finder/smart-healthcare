# Secrets Management Guide

## Overview
This project uses secure secrets management to protect sensitive configuration (API keys, database passwords, etc). Secrets are **never** committed to git.

## Setting Up Your Environment

### Step 1: Get Your OpenAI API Key
1. Go to https://platform.openai.com/api-keys
2. Create a new API key
3. Copy the key (starts with `sk-`)

### Step 2: Configure Locally

#### Option A: Environment Variable (Recommended for Development)
```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY = 'sk-your-actual-key-here'

# Windows (CMD)
set OPENAI_API_KEY=sk-your-actual-key-here

# Linux/macOS
export OPENAI_API_KEY=sk-your-actual-key-here
```

#### Option B: .env File (Local Development)
1. Copy `.env.example` to `.env` (in AIService folder)
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and add your actual API key:
   ```
   OPENAI_API_KEY=sk-your-actual-key-here
   ```

3. ⚠️ **Important**: `.env` is in `.gitignore` - it will NOT be committed

#### Option C: User Secrets (Visual Studio / Rider)
```bash
cd backend/AIService

# Initialize user secrets
dotnet user-secrets init

# Set the API key
dotnet user-secrets set "OpenAI:ApiKey" "sk-your-actual-key-here"

# View all secrets
dotnet user-secrets list
```

### Step 3: Verify Configuration
Run the application:
```bash
cd backend/AIService
dotnet run --urls "http://localhost:8081"
```

You should see:
```
[INF] Using OpenAI API key from environment variable
[INF] ✓ All required secrets validated successfully
```

---

## Security Best Practices

✅ **DO:**
- Use environment variables in production
- Keep `.env` file local (never commit)
- Rotate API keys regularly
- Use different keys for dev/staging/production
- Store in secure vaults (Azure Key Vault, AWS Secrets Manager, etc)

❌ **DON'T:**
- Commit `.env` or `appsettings.Development.json` to git
- Share API keys via email or chat
- Use same key across environments
- Log or print secret values

---

## Secrets Priority (Loading Order)

1. **Environment Variables** (Highest Priority)
   ```
   OPENAI_API_KEY=...
   ```

2. **User Secrets** (Development)
   ```
   dotnet user-secrets set "OpenAI:ApiKey" "..."
   ```

3. **appsettings.Development.json** (Local)
   ```json
   {
     "OpenAI": {
       "ApiKey": "..."
     }
   }
   ```

4. **appsettings.json** (Shared - should be empty)
   ```json
   {
     "OpenAI": {
       "ApiKey": ""
     }
   }
   ```

---

## Troubleshooting

### Error: "OpenAI API key not found"
**Solution**: Check that your environment variable or config has the API key set:
- Check environment: `echo $OPENAI_API_KEY`
- Check appsettings: Verify `OpenAI:ApiKey` is not empty
- Restart your IDE/terminal after setting the environment variable

### Error: "OpenAI API key appears invalid"
**Solution**: The key is too short. Valid keys start with `sk-` and are usually 40+ characters.

### Error: "401 Unauthorized" from OpenAI
**Solution**: Your API key is invalid or expired. Generate a new one from platform.openai.com

---

## Production Deployment

For production, use your cloud provider's secrets management:

- **Azure**: Azure Key Vault
- **AWS**: Secrets Manager
- **Google Cloud**: Secret Manager
- **Docker**: Docker Secrets

Set the environment variable when deploying:
```bash
# Kubernetes
- name: OPENAI_API_KEY
  valueFrom:
    secretKeyRef:
      name: ai-secrets
      key: openai-api-key

# Docker
docker run -e OPENAI_API_KEY=sk-... myapp

# Lambda / Serverless
environment:
  OPENAI_API_KEY: ${openai_api_key}
