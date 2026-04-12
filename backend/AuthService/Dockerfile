FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj
COPY ["AuthService/Auth.API/Auth.API.csproj", "AuthService/Auth.API/"]
COPY ["AuthService/Auth.Application/Auth.Application.csproj", "AuthService/Auth.Application/"]
COPY ["AuthService/Auth.Domain/Auth.Domain.csproj", "AuthService/Auth.Domain/"]
COPY ["AuthService/Auth.Infrastructure/Auth.Infrastructure.csproj", "AuthService/Auth.Infrastructure/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

# restore dependencies
RUN dotnet restore "AuthService/Auth.API/Auth.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "AuthService/Auth.API/Auth.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "Auth.API.dll"]