# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files first so restore is cached when only sources change.
COPY Tip4Gen.sln ./
COPY src/Tip4Gen.Api/Tip4Gen.Api.csproj src/Tip4Gen.Api/
COPY src/Tip4Gen.Domain/Tip4Gen.Domain.csproj src/Tip4Gen.Domain/
COPY src/Tip4Gen.Infrastructure/Tip4Gen.Infrastructure.csproj src/Tip4Gen.Infrastructure/
COPY tests/Tip4Gen.Domain.Tests/Tip4Gen.Domain.Tests.csproj tests/Tip4Gen.Domain.Tests/

RUN dotnet restore src/Tip4Gen.Api/Tip4Gen.Api.csproj

COPY src/ src/

RUN dotnet publish src/Tip4Gen.Api/Tip4Gen.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_PRINT_TELEMETRY_MESSAGE=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "Tip4Gen.Api.dll"]
