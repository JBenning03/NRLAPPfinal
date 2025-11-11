# ---------- BUILD-STEG ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopier alt til containeren
COPY . .

# Gjenopprett avhengigheter og bygg
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ---------- RUN-STEG ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Kopier publiserte filer fra build-steget
COPY --from=build /app/publish .

# Eksponer port 8080 (den Docker vil bruke)
EXPOSE 8080

# Start appen
ENTRYPOINT ["dotnet", "NRLApp.dll"]
