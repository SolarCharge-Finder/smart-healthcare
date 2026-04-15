# Build stage - minimal
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy csproj and restore with cache
COPY "AIService.csproj" "./AIService.csproj"
RUN dotnet restore

# Copy everything and build in Release mode
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage - optimized
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Install curl for health checks
RUN apk add --no-cache curl

# Create non-root user
RUN addgroup -S dotnet && adduser -S dotnet -G dotnet

WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R dotnet:dotnet /app

# Use non-root user
USER dotnet

# Environment
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD curl -f http://localhost:8080/api/ai/health || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "AIService.dll"]
