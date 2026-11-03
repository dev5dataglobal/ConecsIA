# Imagen de la API (ADR-017). Contexto de construcción: carpeta backend/.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ApiConecsia/ApiConecsia.csproj -c Release -o /app /p:UseAppHost=false

# Bundle de migraciones EF Core: aplica solo las migraciones pendientes, nunca borra datos.
FROM build AS migraciones-build
RUN dotnet tool install --global dotnet-ef --version 9.0.6
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle --project Infrastructure/Infrastructure.csproj \
    --startup-project ApiConecsia/ApiConecsia.csproj -o /out/efbundle --self-contained -r linux-x64

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migraciones
WORKDIR /app
COPY --from=migraciones-build /out/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Carpeta de archivos subidos (Almacenamiento:RutaLocal); el volumen hereda este dueño.
RUN mkdir -p /app/App_Data/archivos && chown -R $APP_UID /app/App_Data
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiConecsia.dll"]
