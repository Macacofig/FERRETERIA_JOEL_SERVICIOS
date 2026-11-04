# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# En algunas redes Docker api.nuget.org resuelve a IPv6 sin ruta y el restore
# falla con "unexpected EOF". gai.conf fuerza la preferencia por IPv4.
RUN echo 'precedence ::ffff:0:0/96  100' >> /etc/gai.conf

COPY ServicioVentas.csproj ./
RUN dotnet restore ServicioVentas.csproj

COPY . .
RUN dotnet publish ServicioVentas.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ServicioVentas.dll"]
