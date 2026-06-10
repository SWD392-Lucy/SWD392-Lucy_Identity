# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Lucy.Identity.sln ./
COPY Lucy.Identity.Api/Lucy.Identity.Api.csproj Lucy.Identity.Api/
COPY Lucy.Identity.Domain/Lucy.Identity.Domain.csproj Lucy.Identity.Domain/
COPY Lucy.Identity.Infrastructure/Lucy.Identity.Infrastructure.csproj Lucy.Identity.Infrastructure/

RUN dotnet restore Lucy.Identity.sln

COPY . .
RUN dotnet publish Lucy.Identity.Api/Lucy.Identity.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    LUCY_ENABLE_HTTPS_REDIRECTION=false

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Lucy.Identity.Api.dll"]
