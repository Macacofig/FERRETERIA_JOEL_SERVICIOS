# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FERRETERIA_Web.csproj ./
RUN dotnet restore FERRETERIA_Web.csproj

COPY . .
RUN dotnet publish FERRETERIA_Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FERRETERIA_Web.dll"]
