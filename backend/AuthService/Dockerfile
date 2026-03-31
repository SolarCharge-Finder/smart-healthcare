FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj
COPY ["Auth.API/Auth.API.csproj", "Auth.API/"]

# restore dependencies
RUN dotnet restore "Auth.API/Auth.API.csproj"

# copy everything else
COPY . .

# publish ONLY API project
RUN dotnet publish "Auth.API/Auth.API.csproj" -c Release -o /app/publish

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# entrypoint
ENTRYPOINT ["dotnet", "Auth.API.dll"]