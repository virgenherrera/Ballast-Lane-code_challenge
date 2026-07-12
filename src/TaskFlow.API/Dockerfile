# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TaskFlow.sln ./
COPY src/TaskFlow.Domain/TaskFlow.Domain.csproj src/TaskFlow.Domain/
COPY src/TaskFlow.Application/TaskFlow.Application.csproj src/TaskFlow.Application/
COPY src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj src/TaskFlow.Infrastructure/
COPY src/TaskFlow.API/TaskFlow.API.csproj src/TaskFlow.API/
COPY tests/TaskFlow.Domain.Tests/TaskFlow.Domain.Tests.csproj tests/TaskFlow.Domain.Tests/
COPY tests/TaskFlow.Application.Tests/TaskFlow.Application.Tests.csproj tests/TaskFlow.Application.Tests/
COPY tests/TaskFlow.Infrastructure.Tests/TaskFlow.Infrastructure.Tests.csproj tests/TaskFlow.Infrastructure.Tests/
COPY tests/TaskFlow.IntegrationTests/TaskFlow.IntegrationTests.csproj tests/TaskFlow.IntegrationTests/
COPY tests/TaskFlow.E2E/TaskFlow.E2E.csproj tests/TaskFlow.E2E/

RUN dotnet restore

COPY src/ src/
COPY tests/ tests/
RUN dotnet publish src/TaskFlow.API/TaskFlow.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]
