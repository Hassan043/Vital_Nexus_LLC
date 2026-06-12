# Build context: backend/ (repository root: backend/)
# Example: docker build -f src/Workers/VitalNexus.AiAnalysis.Worker/Dockerfile -t vitalnexus-ai-analysis-worker .

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
COPY ["src/Workers/VitalNexus.AiAnalysis.Worker/VitalNexus.AiAnalysis.Worker.csproj", "src/Workers/VitalNexus.AiAnalysis.Worker/"]

RUN dotnet restore "src/Workers/VitalNexus.AiAnalysis.Worker/VitalNexus.AiAnalysis.Worker.csproj"

COPY . .
WORKDIR "/src/src/Workers/VitalNexus.AiAnalysis.Worker"
RUN dotnet build "VitalNexus.AiAnalysis.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "VitalNexus.AiAnalysis.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VitalNexus.AiAnalysis.Worker.dll"]
