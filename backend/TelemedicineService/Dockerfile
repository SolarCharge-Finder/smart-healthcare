FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TelemedicineService.csproj ./
RUN dotnet restore TelemedicineService.csproj

COPY . .
RUN dotnet build TelemedicineService.csproj -c Release
RUN dotnet publish TelemedicineService.csproj -c Release -o /app/publish --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "TelemedicineService.dll"]
