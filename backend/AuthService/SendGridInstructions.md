# SendGrid Setup and Kubernetes Secret Configuration

This guide walks through creating a SendGrid account, generating an API key, storing it securely in Kubernetes, and verifying the setup.

---

## 1. Create a SendGrid Account

1. Go to https://sendgrid.com/
2. Click **Sign Up**
3. Complete registration

---

## 2. Generate a SendGrid API Key

1. Log in to your SendGrid dashboard
2. Navigate to **Settings → API Keys**
3. Click **Create API Key**
4. Choose:

   * **Name**: e.g. `smarthealth-app`
   * **Permissions**: Select **Custom Access** ( Give Mail Send -> Full Access) 
   * you can use full access but its not ideal
5. Click **Create & View**
6. Copy the API key immediately (you won’t be able to see it again)

---

## 3. Update Kubernetes Secret YAML

Create or update your `secrets.yaml` file like this:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: smarthealth-secrets
type: Opaque
stringData:
  POSTGRES_USER: "change-me"
  POSTGRES_PASSWORD: "change-me"
  RABBITMQ_DEFAULT_USER: "change-me"
  RABBITMQ_DEFAULT_PASS: "change-me"
  REDIS_PASSWORD: "change-me"
  API_KEY: "change-me"

--- #this part is important btw or you can just set send grid part in a different sendgridsecrets.yaml too
apiVersion: v1
kind: Secret
metadata:
  name: sendgrid-secret
type: Opaque
stringData:
  SendGrid__ApiKey: "your-sendgrid-api-key"
  SendGrid__FromEmail: "your-email@example.com"
  SendGrid__FromName: "SmartHealth"
```
* Either way dont push the SendGrid API data to github you can just discard changes in version control after applying (check below)

### Notes

* `stringData` allows plain text (no base64 encoding needed)
* The `---` separator is required to define multiple resources in one file
* The double underscore (`__`) is used for hierarchical config in ASP.NET

---

## 4. Apply Secrets to Kubernetes

Run:

```bash
kubectl apply -f secrets.yaml
```

If successful, you should see:

```
secret/smarthealth-secrets created
secret/sendgrid-secret created
```

---

## 5. Verify Secrets

### List all secrets

```bash
kubectl get secrets
```

You should see:

```
smarthealth-secrets
sendgrid-secret
```

---

### Describe the SendGrid secret

```bash
kubectl describe secret sendgrid-secret
```

This shows:

* Metadata
* Keys (but not decoded values)

Example output snippet:

```
Data
====
SendGrid__ApiKey:     55 bytes
SendGrid__FromEmail:  22 bytes
SendGrid__FromName:   11 bytes
```

---

### (Optional) View encoded values

```bash
kubectl get secret sendgrid-secret -o yaml
```

---

## 6. Use in Deployment (not necessary for you guys to test auth)

To inject into your application:

```yaml
envFrom:
  - secretRef:
      name: sendgrid-secret
```

---

## 7. Access in ASP.NET (not necessary for you guys to test auth)

In your application code:

```csharp
var apiKey = Environment.GetEnvironmentVariable("SendGrid__ApiKey");
```

---

## Important Notes

* Do not commit real API keys to Git repositories
* Use different keys for development and production
* Restart deployments after updating secrets:

```bash
kubectl rollout restart deployment auth-service
```

---

This setup ensures your SendGrid credentials are securely managed and easily injected into your services.
