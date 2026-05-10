FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY src/SearchEngine/SearchEngine.csproj src/SearchEngine/
COPY src/SearchEngine.Service/SearchEngine.Service.csproj src/SearchEngine.Service/

RUN dotnet restore src/SearchEngine.Service/SearchEngine.Service.csproj

COPY src/SearchEngine/ src/SearchEngine/
COPY src/SearchEngine.Service/ src/SearchEngine.Service/

RUN dotnet publish src/SearchEngine.Service/SearchEngine.Service.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["dotnet", "SearchEngine.Service.dll", "--healthcheck"]

ENTRYPOINT ["dotnet", "SearchEngine.Service.dll"]