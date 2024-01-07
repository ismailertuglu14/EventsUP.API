#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
EXPOSE 80
COPY ["Services/Authentication/Topluluk.Services.AuthenticationAPI/Topluluk.Services.AuthenticationAPI.csproj", "Services/Authentication/Topluluk.Services.AuthenticationAPI/"]
COPY ["Services/Authentication/Topluluk.Services.AuthenticationAPI.Data/Topluluk.Services.AuthenticationAPI.Data.csproj", "Services/Authentication/Topluluk.Services.AuthenticationAPI.Data/"]
COPY ["Shared/DBHelper/DBHelper.csproj", "Shared/DBHelper/"]
COPY ["Shared/Topluluk.Shared/Topluluk.Shared.csproj", "Shared/Topluluk.Shared/"]
COPY ["Services/Authentication/Topluluk.Services.AuthenticationAPI.Model/Topluluk.Services.AuthenticationAPI.Model.csproj", "Services/Authentication/Topluluk.Services.AuthenticationAPI.Model/"]
COPY ["Services/Authentication/Topluluk.Services.AuthenticationAPI.Services/Topluluk.Services.AuthenticationAPI.Services.csproj", "Services/Authentication/Topluluk.Services.AuthenticationAPI.Services/"]
RUN dotnet restore "Services/Authentication/Topluluk.Services.AuthenticationAPI/Topluluk.Services.AuthenticationAPI.csproj"
COPY . .
WORKDIR "/src/Services/Authentication/Topluluk.Services.AuthenticationAPI"
RUN dotnet build "Topluluk.Services.AuthenticationAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Topluluk.Services.AuthenticationAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Topluluk.Services.AuthenticationAPI.dll"]