#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Services/Post/Topluluk.Services.PostAPI/Topluluk.Services.PostAPI.csproj", "Services/Post/Topluluk.Services.PostAPI/"]
COPY ["Shared/Topluluk.Shared/Topluluk.Shared.csproj", "Shared/Topluluk.Shared/"]
COPY ["Services/Post/Topluluk.Services.PostAPI.Services/Topluluk.Services.PostAPI.Services.csproj", "Services/Post/Topluluk.Services.PostAPI.Services/"]
COPY ["Services/Post/Topluluk.Services.PostAPI.Model/Topluluk.Services.PostAPI.Model.csproj", "Services/Post/Topluluk.Services.PostAPI.Model/"]
COPY ["Shared/DBHelper/DBHelper.csproj", "Shared/DBHelper/"]
COPY ["Services/Post/Topluluk.Services.PostAPI.Data/Topluluk.Services.PostAPI.Data.csproj", "Services/Post/Topluluk.Services.PostAPI.Data/"]
RUN dotnet restore "Services/Post/Topluluk.Services.PostAPI/Topluluk.Services.PostAPI.csproj"
COPY . .
WORKDIR "/src/Services/Post/Topluluk.Services.PostAPI"
RUN dotnet build "Topluluk.Services.PostAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Topluluk.Services.PostAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Topluluk.Services.PostAPI.dll"]