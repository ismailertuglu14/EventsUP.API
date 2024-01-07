#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Services/Community/Topluluk.Services.CommunityAPI/Topluluk.Services.CommunityAPI.csproj", "Services/Community/Topluluk.Services.CommunityAPI/"]
COPY ["Services/Community/Topluluk.Services.CommunityAPI.Services/Topluluk.Services.CommunityAPI.Services.csproj", "Services/Community/Topluluk.Services.CommunityAPI.Services/"]
COPY ["Shared/Topluluk.Shared/Topluluk.Shared.csproj", "Shared/Topluluk.Shared/"]
COPY ["Services/Community/Topluluk.Services.CommunityAPI.Model/Topluluk.Services.CommunityAPI.Model.csproj", "Services/Community/Topluluk.Services.CommunityAPI.Model/"]
COPY ["Services/Community/Topluluk.Services.CommunityAPI.Data/Topluluk.Services.CommunityAPI.Data.csproj", "Services/Community/Topluluk.Services.CommunityAPI.Data/"]
COPY ["Shared/DBHelper/DBHelper.csproj", "Shared/DBHelper/"]
RUN dotnet restore "Services/Community/Topluluk.Services.CommunityAPI/Topluluk.Services.CommunityAPI.csproj"
COPY . .
WORKDIR "/src/Services/Community/Topluluk.Services.CommunityAPI"
RUN dotnet build "Topluluk.Services.CommunityAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Topluluk.Services.CommunityAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Topluluk.Services.CommunityAPI.dll"]