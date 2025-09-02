
# See https://aka.ms/customizecontainer to learn how to customize your debug container
# and https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything and restore as distinct layers
COPY ["AIProjectOrchestrator.sln", "."]
COPY ["src/AIProjectOrchestrator.API/AIProjectOrchestrator.API.csproj", "src/AIProjectOrchestrator.API/"]
COPY ["src/AIProjectOrchestrator.Application/AIProjectOrchestrator.Application.csproj", "src/AIProjectOrchestrator.Application/"]
COPY ["src/AIProjectOrchestrator.Domain/AIProjectOrchestrator.Domain.csproj", "src/AIProjectOrchestrator.Domain/"]
COPY ["src/AIProjectOrchestrator.Infrastructure/AIProjectOrchestrator.Infrastructure.csproj", "src/AIProjectOrchestrator.Infrastructure/"]

# Copy instruction files
COPY Instructions ./Instructions

RUN dotnet restore "./src/AIProjectOrchestrator.API/AIProjectOrchestrator.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/AIProjectOrchestrator.API"
RUN dotnet build "AIProjectOrchestrator.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "AIProjectOrchestrator.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIProjectOrchestrator.API.dll"]