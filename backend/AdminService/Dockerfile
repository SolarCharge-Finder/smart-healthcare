FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj files
COPY ["AdminService/AdminService.API/AdminService.API.csproj", "AdminService/AdminService.API/"]
COPY ["AdminService/AdminService.Application/AdminService.Application.csproj", "AdminService/AdminService.Application/"]
COPY ["AdminService/AdminService.Domain/AdminService.Domain.csproj", "AdminService/AdminService.Domain/"]
COPY ["AdminService/AdminService.Infrastructure/AdminService.Infrastructure.csproj", "AdminService/AdminService.Infrastructure/"]

COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

# restore dependencies
RUN dotnet restore "AdminService/AdminService.API/AdminService.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "AdminService/AdminService.API/AdminService.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "AdminService.API.dll"]