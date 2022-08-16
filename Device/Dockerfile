#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Device/Device.csproj", "Device/"]
COPY ["SharedLib/SharedLib.csproj", "SharedLib/"]
RUN dotnet restore "Device/Device.csproj"
COPY . .
WORKDIR "/src/Device"
RUN dotnet build "Device.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Device.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Device.dll"]