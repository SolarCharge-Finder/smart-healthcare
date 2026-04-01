FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj files
COPY [".API/SmartService.API.csproj", "SmartService.API/"]
COPY ["SmartService.Application/SmartService.Application.csproj", "SmartService.Application/"]
COPY ["SmartService.Domain/SmartService.Domain.csproj", "SmartService.Domain/"]
COPY ["SmartService.Infrastructure/SmartService.Infrastructure.csproj", "SmartService.Infrastructure/"]

# restore dependencies
RUN dotnet restore "SmartService.API/SmartService.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "SmartService.API/SmartService.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "SmartService.API.dll"]