FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Microservices/ContentCreator/ContentCreator.API/ContentCreator.API.csproj", "Microservices/ContentCreator/ContentCreator.API/"]
COPY ["Microservices/ContentCreator/ContentCreator.Application/ContentCreator.Application.csproj", "Microservices/ContentCreator/ContentCreator.Application/"]
COPY ["Microservices/ContentCreator/ContentCreator.Domain/ContentCreator.Domain.csproj", "Microservices/ContentCreator/ContentCreator.Domain/"]
COPY ["Microservices/ContentCreator/ContentCreator.Infrastructure/ContentCreator.Infrastructure.csproj", "Microservices/ContentCreator/ContentCreator.Infrastructure/"]
COPY ["Microservices/ContentCreator/ContentCreator.Shared/ContentCreator.Shared.csproj", "Microservices/ContentCreator/ContentCreator.Shared/"]
RUN dotnet restore "Microservices/ContentCreator/ContentCreator.API/ContentCreator.API.csproj"
COPY . .
WORKDIR "/src/Microservices/ContentCreator/ContentCreator.API"
RUN dotnet build "ContentCreator.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ContentCreator.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContentCreator.API.dll"] 