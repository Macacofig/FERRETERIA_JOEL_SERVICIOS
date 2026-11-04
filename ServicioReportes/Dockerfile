# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# En algunas redes Docker api.nuget.org resuelve a IPv6 sin ruta y el restore
# falla con "unexpected EOF". gai.conf fuerza la preferencia por IPv4.
RUN echo 'precedence ::ffff:0:0/96  100' >> /etc/gai.conf

COPY ServicioReportes.csproj ./
RUN dotnet restore ServicioReportes.csproj

COPY . .
RUN dotnet publish ServicioReportes.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# QuestPDF/SkiaSharp necesita fontconfig y al menos una familia tipográfica
# disponible en Linux para rasterizar el gráfico y el logotipo del reporte.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        fontconfig \
        libfontconfig1 \
        fonts-dejavu-core \
        fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ServicioReportes.dll"]
