FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj files
COPY ["AdminService.API/AdminService.API.csproj", "AdminService.API/"]
COPY ["AdminService.Application/AdminService.Application.csproj", "AdminService.Application/"]
COPY ["AdminService.Domain/AdminService.Domain.csproj", "AdminService.Domain/"]
COPY ["AdminService.Infrastructure/AdminService.Infrastructure.csproj", "AdminService.Infrastructure/"]

# restore dependencies
RUN dotnet restore "AdminService.API/AdminService.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "AdminService.API/AdminService.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "AdminService.API.dll"]