FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy project files
COPY . .

# restore dependencies
RUN dotnet restore

# publish optimized build
RUN dotnet publish -c Release -o out


# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AppointmentService.dll"]