FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Examples/Felis.Broker.Mqtt.Console/Felis.Broker.Mqtt.Console.csproj", "Felis.Broker.Mqtt.Console/"]
RUN dotnet restore "Examples/Felis.Broker.Mqtt.Console/Felis.Broker.Mqtt.Console.csproj"
COPY . .
WORKDIR "/src/Felis.Broker.Mqtt.Console"
RUN dotnet build "Felis.Broker.Mqtt.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Felis.Broker.Mqtt.Console.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Felis.Broker.Mqtt.Console.dll"]
