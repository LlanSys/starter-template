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
COPY ["src/Frontend/Web/ST.UI.Blazor/ST.UI.Blazor.csproj", "src/Frontend/Web/ST.UI.Blazor/"]

RUN dotnet restore "src/Frontend/Web/ST.UI.Blazor/ST.UI.Blazor.csproj"

COPY ["src/Shared/", "src/Shared/"]
COPY ["src/Frontend/", "src/Frontend/"]

RUN dotnet publish "src/Frontend/Web/ST.UI.Blazor/ST.UI.Blazor.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# Static web assets can reference framework files from the SDK package cache.
# Copy them into the runtime image so containerized Blazor can serve /_framework/*.
RUN framework_asset="$(find / -path '*/_framework/blazor.web.js' -print -quit)" \
    && test -n "$framework_asset" \
    && framework_asset_dir="$(dirname "$framework_asset")" \
    && mkdir -p /app/publish/wwwroot/_framework \
    && cp "$framework_asset_dir"/blazor.web* /app/publish/wwwroot/_framework/

FROM runtime AS final
WORKDIR /app
RUN mkdir -p /var/startertemplate/dataprotection \
    && chown -R $APP_UID:$APP_UID /var/startertemplate
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "ST.UI.Blazor.dll"]
