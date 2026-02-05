FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Pms.Api/Pms.Api.csproj", "Pms.Api/"]
COPY ["src/Pms.Application/Pms.Application.csproj", "Pms.Application/"]
COPY ["src/Pms.Domain/Pms.Domain.csproj", "Pms.Domain/"]
COPY ["src/Pms.Infrastructure/Pms.Infrastructure.csproj", "Pms.Infrastructure/"]
RUN dotnet restore "Pms.Api/Pms.Api.csproj"
COPY src/ .
WORKDIR "/src/Pms.Api"
RUN dotnet build "Pms.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pms.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pms.Api.dll"]
