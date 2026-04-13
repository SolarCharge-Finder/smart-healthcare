FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj files
COPY ["PatientService/PatientService.API/PatientService.API.csproj", "PatientService/PatientService.API/"]
COPY ["PatientService/PatientService.Application/PatientService.Application.csproj", "PatientService/PatientService.Application/"]
COPY ["PatientService/PatientService.Domain/PatientService.Domain.csproj", "PatientService/PatientService.Domain/"]
COPY ["PatientService/PatientService.Infrastructure/PatientService.Infrastructure.csproj", "PatientService/PatientService.Infrastructure/"]

# restore dependencies
RUN dotnet restore "PatientService/PatientService.API/PatientService.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "PatientService/PatientService.API/PatientService.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "PatientService.API.dll"]