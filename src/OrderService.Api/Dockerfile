FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/OrderService.Domain/OrderService.Domain.csproj OrderService.Domain/
COPY src/OrderService.Application/OrderService.Application.csproj OrderService.Application/
COPY src/OrderService.Infrastructure/OrderService.Infrastructure.csproj OrderService.Infrastructure/
COPY src/OrderService.Api/OrderService.Api.csproj OrderService.Api/
RUN dotnet restore OrderService.Api/OrderService.Api.csproj
COPY src/OrderService.Domain/ OrderService.Domain/
COPY src/OrderService.Application/ OrderService.Application/
COPY src/OrderService.Infrastructure/ OrderService.Infrastructure/
COPY src/OrderService.Api/ OrderService.Api/
RUN dotnet publish OrderService.Api/OrderService.Api.csproj --configuration Release --output /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=5064
EXPOSE 5064
USER $APP_UID
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]
