FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ShipmentService.Api/ShipmentService.Api.csproj ShipmentService.Api/
RUN dotnet restore ShipmentService.Api/ShipmentService.Api.csproj
COPY src/ShipmentService.Api/ ShipmentService.Api/
RUN dotnet publish ShipmentService.Api/ShipmentService.Api.csproj --configuration Release --output /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=5065
EXPOSE 5065
USER $APP_UID
ENTRYPOINT ["dotnet", "ShipmentService.Api.dll"]
