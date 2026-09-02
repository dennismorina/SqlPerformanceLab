FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY src/SqlPerformanceLab.Core/SqlPerformanceLab.Core.csproj src/SqlPerformanceLab.Core/
COPY src/SqlPerformanceLab.Runner/SqlPerformanceLab.Runner.csproj src/SqlPerformanceLab.Runner/

RUN dotnet restore src/SqlPerformanceLab.Runner/SqlPerformanceLab.Runner.csproj

COPY src ./src
COPY sql ./sql

RUN dotnet publish src/SqlPerformanceLab.Runner/SqlPerformanceLab.Runner.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SqlPerformanceLab.Runner.dll"]
