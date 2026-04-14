FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj files
COPY ["DoctorService/Doctor.API/Doctor.API.csproj", "DoctorService/Doctor.API/"]
COPY ["DoctorService/Doctor.Application/Doctor.Application.csproj", "DoctorService/Doctor.Application/"]
COPY ["DoctorService/Doctor.Domain/Doctor.Domain.csproj", "DoctorService/Doctor.Domain/"]
COPY ["DoctorService/Doctor.Infrastructure/Doctor.Infrastructure.csproj", "DoctorService/Doctor.Infrastructure/"]

COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

# restore dependencies
RUN dotnet restore "DoctorService/Doctor.API/Doctor.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "DoctorService/Doctor.API/Doctor.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "Doctor.API.dll"]