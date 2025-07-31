# ===== STAGE 1: Build =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY OrderBookAlgorithm/OrderBookAlgorithm.csproj OrderBookAlgorithm/
COPY OrderBookApi/OrderBookApi.csproj OrderBookApi/
RUN dotnet restore OrderBookApi/OrderBookApi.csproj

COPY . .
RUN dotnet publish OrderBookApi/OrderBookApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== STAGE 2: Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderBookApi.dll"]
