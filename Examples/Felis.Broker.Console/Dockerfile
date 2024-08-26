FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Examples/Felis.Broker.Console/Felis.Broker.Console.csproj", "Examples/Felis.Broker.Console/"]
COPY ["Felis/Felis.csproj", "Felis/"]
RUN dotnet restore "Examples/Felis.Broker.Console/Felis.Broker.Console.csproj"
COPY . .
WORKDIR "/src/Examples/Felis.Broker.Console"
RUN dotnet build "Felis.Broker.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Felis.Broker.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Expose the HTTPS port
EXPOSE 7110

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Felis.Broker.Console.dll"]
