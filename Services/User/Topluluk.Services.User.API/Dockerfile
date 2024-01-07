#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Services/User/Topluluk.Services.User.API/Topluluk.Services.User.API.csproj", "Services/User/Topluluk.Services.User.API/"]
COPY ["Services/User/Topluluk.Services.User.Services/Topluluk.Services.User.Services.csproj", "Services/User/Topluluk.Services.User.Services/"]
COPY ["Shared/Topluluk.Shared/Topluluk.Shared.csproj", "Shared/Topluluk.Shared/"]
COPY ["Shared/DBHelper/DBHelper.csproj", "Shared/DBHelper/"]
COPY ["Services/User/Topluluk.Services.User.Data/Topluluk.Services.User.Data.csproj", "Services/User/Topluluk.Services.User.Data/"]
COPY ["Services/User/Topluluk.Services.User.Model/Topluluk.Services.User.Model.csproj", "Services/User/Topluluk.Services.User.Model/"]
RUN dotnet restore "Services/User/Topluluk.Services.User.API/Topluluk.Services.User.API.csproj"
COPY . .
WORKDIR "/src/Services/User/Topluluk.Services.User.API"
RUN dotnet build "Topluluk.Services.User.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Topluluk.Services.User.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Topluluk.Services.User.API.dll"]