# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CityInfo.API.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage (use 'base' for consistency)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_GENERATE_ASPNET_CERTIFICATE=false

# Copy published files
COPY --from=build /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "CityInfo.API.dll"]