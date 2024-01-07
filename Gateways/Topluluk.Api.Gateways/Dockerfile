#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
EXPOSE 80
WORKDIR /src
COPY ["Gateways/Topluluk.Api.Gateways/Topluluk.Api.Gateways.csproj", "Gateways/Topluluk.Api.Gateways/"]
RUN dotnet restore "Gateways/Topluluk.Api.Gateways/Topluluk.Api.Gateways.csproj"
COPY . .
WORKDIR "/src/Gateways/Topluluk.Api.Gateways"
RUN dotnet build "Topluluk.Api.Gateways.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Topluluk.Api.Gateways.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Topluluk.Api.Gateways.dll"]