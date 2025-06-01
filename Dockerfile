FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "ReembolsoBAS.csproj"
RUN dotnet build "ReembolsoBAS.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReembolsoBAS.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReembolsoBAS.dll"]