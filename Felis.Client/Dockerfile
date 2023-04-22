FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Felis.Client/Felis.Client.csproj", "Felis.Client/"]
RUN dotnet restore "Felis.Client/Felis.Client.csproj"
COPY . .
WORKDIR "/src/Felis.Client"
RUN dotnet build "Felis.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Felis.Client.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Felis.Client.dll"]
