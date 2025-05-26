FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Microservices/Authentication/Authentication.API/Authentication.API.csproj", "Microservices/Authentication/Authentication.API/"]
COPY ["Microservices/Authentication/Authentication.Application/Authentication.Application.csproj", "Microservices/Authentication/Authentication.Application/"]
COPY ["Microservices/Authentication/Authentication.Domain/Authentication.Domain.csproj", "Microservices/Authentication/Authentication.Domain/"]
COPY ["Microservices/Authentication/Authentication.Infrastructure/Authentication.Infrastructure.csproj", "Microservices/Authentication/Authentication.Infrastructure/"]
COPY ["Microservices/Authentication/Authentication.Shared/Authentication.Shared.csproj", "Microservices/Authentication/Authentication.Shared/"]
RUN dotnet restore "Microservices/Authentication/Authentication.API/Authentication.API.csproj"
COPY . .
WORKDIR "/src/Microservices/Authentication/Authentication.API"
RUN dotnet build "Authentication.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Authentication.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Authentication.API.dll"] 