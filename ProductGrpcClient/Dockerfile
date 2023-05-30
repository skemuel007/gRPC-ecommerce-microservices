FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ProductGrpcClient/ProductGrpcClient.csproj", "ProductGrpcClient/"]
RUN dotnet restore "ProductGrpcClient/ProductGrpcClient.csproj"
COPY . .
WORKDIR "/src/ProductGrpcClient"
RUN dotnet build "ProductGrpcClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductGrpcClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductGrpcClient.dll"]
