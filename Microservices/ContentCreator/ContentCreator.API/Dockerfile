FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Microservices/ContentCreator/ContentCreator.API/ContentCreator.API.csproj", "Microservices/ContentCreator/ContentCreator.API/"]
RUN dotnet restore "Microservices/ContentCreator/ContentCreator.API/ContentCreator.API.csproj"
COPY . .
WORKDIR "/src/Microservices/ContentCreator/ContentCreator.API"
RUN dotnet build "ContentCreator.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ContentCreator.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContentCreator.API.dll"] 