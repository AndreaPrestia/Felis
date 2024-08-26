FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Examples/Felis.Publisher.Net.Console/Felis.Publisher.Net.Console.csproj", "Examples/Felis.Publisher.Net.Console/"]
RUN dotnet restore "Examples/Felis.Publisher.Net.Console/Felis.Publisher.Net.Console.csproj"
COPY . .
WORKDIR "/src/Examples/Felis.Publisher.Net.Console"
RUN dotnet build "Felis.Publisher.Net.Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Felis.Publisher.Net.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Felis.Publisher.Net.Console.dll"]
