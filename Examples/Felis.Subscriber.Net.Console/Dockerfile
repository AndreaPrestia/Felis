FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Examples/Felis.Subscriber.Net.Console/Felis.Subscriber.Net.Console.csproj", "Examples/Felis.Subscriber.Net.Console/"]
RUN dotnet restore "Examples/Felis.Subscriber.Net.Console/Felis.Subscriber.Net.Console.csproj"
COPY . .
WORKDIR "/src/Examples/Felis.Subscriber.Net.Console"
RUN dotnet build "Felis.Subscriber.Net.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Felis.Subscriber.Net.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Felis.Subscriber.Net.Console.dll"]
