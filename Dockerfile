#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/nightly/sdk:7.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "FlightPatternDetection/FlightPatternDetection.csproj"

WORKDIR "/src/FlightPatternDetection"
RUN dotnet build "FlightPatternDetection.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "FlightPatternDetection.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Amazon
ENTRYPOINT ["dotnet", "FlightPatternDetection.dll"]