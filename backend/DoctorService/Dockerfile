FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj files
COPY ["Doctor.API/Doctor.API.csproj", "Doctor.API/"]
COPY ["Doctor.Application/Doctor.Application.csproj", "Doctor.Application/"]
COPY ["Doctor.Domain/Doctor.Domain.csproj", "Doctor.Domain/"]
COPY ["Doctor.Infrastructure/Doctor.Infrastructure.csproj", "Doctor.Infrastructure/"]

# restore dependencies
RUN dotnet restore "Doctor.API/Doctor.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "Doctor.API/Doctor.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "Doctor.API.dll"]