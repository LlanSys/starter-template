# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Global.json", "."]
COPY ["nuget.config", "."]
COPY ["src/Shared/ST.SharedKernel/ST.SharedKernel.csproj", "src/Shared/ST.SharedKernel/"]
COPY ["src/Shared/ST.SharedKernel.Validation/ST.SharedKernel.Validation.csproj", "src/Shared/ST.SharedKernel.Validation/"]
COPY ["src/Backend/Domain/ST.Domain/ST.Domain.csproj", "src/Backend/Domain/ST.Domain/"]
COPY ["src/Backend/Application/ST.Application/ST.Application.csproj", "src/Backend/Application/ST.Application/"]
COPY ["src/Backend/Infrastructure/ST.Infrastructure/ST.Infrastructure.csproj", "src/Backend/Infrastructure/ST.Infrastructure/"]
COPY ["src/Backend/Persistence/ST.Persistence/ST.Persistence.csproj", "src/Backend/Persistence/ST.Persistence/"]
COPY ["src/Backend/Api/ST.Api/ST.Api.csproj", "src/Backend/Api/ST.Api/"]

RUN dotnet restore "src/Backend/Api/ST.Api/ST.Api.csproj"

COPY ["src/Shared/", "src/Shared/"]
COPY ["src/Backend/", "src/Backend/"]

RUN dotnet publish "src/Backend/Api/ST.Api/ST.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ST.Api.dll"]
