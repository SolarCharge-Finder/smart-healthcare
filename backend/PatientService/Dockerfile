FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj files
COPY ["PatientService.API/PatientService.API.csproj", "PatientService.API/"]
COPY ["PatientService.Application/PatientService.Application.csproj", "PatientService.Application/"]
COPY ["PatientService.Domain/PatientService.Domain.csproj", "PatientService.Domain/"]
COPY ["PatientService.Infrastructure/PatientService.Infrastructure.csproj", "PatientService.Infrastructure/"]

# restore dependencies
RUN dotnet restore "PatientService.API/PatientService.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "PatientService.API/PatientService.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "PatientService.API.dll"]