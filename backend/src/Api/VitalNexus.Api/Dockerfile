# Build context: backend/
# Example: docker build -f src/Api/VitalNexus.Api/Dockerfile -t vitalnexus-api:local .

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/Core/VitalNexus.Domain/VitalNexus.Domain.csproj", "src/Core/VitalNexus.Domain/"]
COPY ["src/Core/VitalNexus.Application/VitalNexus.Application.csproj", "src/Core/VitalNexus.Application/"]
COPY ["src/Infrastructure/VitalNexus.Infrastructure/VitalNexus.Infrastructure.csproj", "src/Infrastructure/VitalNexus.Infrastructure/"]
COPY ["src/Api/VitalNexus.Api/VitalNexus.Api.csproj", "src/Api/VitalNexus.Api/"]

RUN dotnet restore "src/Api/VitalNexus.Api/VitalNexus.Api.csproj"

COPY . .
WORKDIR "/src/src/Api/VitalNexus.Api"
RUN dotnet build "VitalNexus.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VitalNexus.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VitalNexus.Api.dll"]
