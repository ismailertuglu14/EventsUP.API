#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Services/Event/Topluluk.Services.EventAPI/Topluluk.Services.EventAPI.csproj", "Services/Event/Topluluk.Services.EventAPI/"]
COPY ["Shared/Topluluk.Shared/Topluluk.Shared.csproj", "Shared/Topluluk.Shared/"]
COPY ["Services/Event/Topluluk.Services.EventAPI.Services/Topluluk.Services.EventAPI.Services.csproj", "Services/Event/Topluluk.Services.EventAPI.Services/"]
COPY ["Services/Event/Topluluk.Services.EventAPI.Model/Topluluk.Services.EventAPI.Model.csproj", "Services/Event/Topluluk.Services.EventAPI.Model/"]
COPY ["Shared/DBHelper/DBHelper.csproj", "Shared/DBHelper/"]
COPY ["Services/Event/Topluluk.Services.EventAPI.Data/Topluluk.Services.EventAPI.Data.csproj", "Services/Event/Topluluk.Services.EventAPI.Data/"]
RUN dotnet restore "Services/Event/Topluluk.Services.EventAPI/Topluluk.Services.EventAPI.csproj"
COPY . .
WORKDIR "/src/Services/Event/Topluluk.Services.EventAPI"
RUN dotnet build "Topluluk.Services.EventAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Topluluk.Services.EventAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Topluluk.Services.EventAPI.dll"]