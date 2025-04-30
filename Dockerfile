#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EasyGameProxy.csproj", "."]
RUN dotnet restore "./EasyGameProxy.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./EasyGameProxy.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EasyGameProxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Give write permission to the file
COPY ./config /config
USER root
RUN mkdir -p /app/keys
RUN chown app:app /app/config
RUN chown app:app /app/keys
RUN chown app:app /app/config/routes.json
USER app
ENTRYPOINT ["dotnet", "EasyGameProxy.dll"]