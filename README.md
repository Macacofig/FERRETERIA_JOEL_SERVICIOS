# FERRETERÍA JOEL

Sistema administrativo para ferretería, organizado como **microservicios** con
ASP.NET Core (.NET 10), Razor Pages y MySQL 8.

## Servicios

| Servicio | Proyecto | Puerto host | Responsabilidad |
|---|---|---|---|
| Usuarios | `ServicioUsuarios` | `5072` | Login, JWT, empleados y sesiones |
| Ventas | `ServicioVentas` | `5071` | Registro, historial, comprobantes y anulación |
| Reportes | `ServicioReportes` | `5073` | Reportes detallado y estadístico, exportación PDF/Excel |
| Web | `FERRETERIA_Web` | `5267` | Interfaz Razor Pages |
| Base de datos | MySQL 8 | `3307`* | Base compartida `bdferreteria` |

## Módulo de Reportes (Entregable 3)

- **Reporte detallado de ventas** — `/Reportes/Ventas`: listado numerado con
  cliente, vendedor, categoría, producto, cantidades, precios, estado, filtros
  por fecha/categoría/estado y totales.
- **Informe estadístico de ventas** — `/Reportes/Estadisticas`: agregado por
  categoría con `GROUP BY`, `COUNT`, `SUM`, participación, gráfico Chart.js y
  tabla de resumen.
- **Exportación PDF** con QuestPDF y **Excel `.xlsx`** con ClosedXML, ambas con
  membrete, logotipo, colores institucionales, paginación, fecha/hora de emisión,
  responsable abreviado y totales al pie.

Documentación completa del entregable:
[`FERRETERIA_Web/Documentacion/Reporte_Entregable3_Reportes.md`](FERRETERIA_Web/Documentacion/Reporte_Entregable3_Reportes.md)

## Arquitectura

`ServicioReportes` sigue arquitectura hexagonal:

```
Dominio          → constantes y utilidades sin dependencias externas
Aplicacion       → modelos, puertos, excepciones y FachadaReportes
Infraestructura  → MySQL (ADO.NET), QuestPDF y ClosedXML
Controllers      → API REST y FileResult
FERRETERIA_Web   → Razor Pages que consumen la API por HTTP con JWT
```

El SQL vive únicamente en `ServicioReportes/Infraestructura/Repositorios`; las
páginas y los controladores nunca acceden directamente a la base de datos.

## Requisitos

- .NET SDK 10
- Docker Desktop y Docker Compose (opción recomendada)
- MySQL 8 si se ejecuta sin Docker

## Configuración

Crear un archivo `.env` en la raíz (no se versiona) con las variables que usa
`docker-compose.yml`:

```env
MYSQL_ROOT_PASSWORD=...
MYSQL_DATABASE=bdferreteria
Jwt__Key=...
Jwt__Issuer=FerreteriaJoel
Jwt__Audience=FerreteriaJoel
```

Variables opcionales: `MYSQL_PUERTO_HOST` (por defecto `3307`; el entorno de
desarrollo actual usa `3308`), `SERVICIO_VENTAS_PUERTO`,
`SERVICIO_USUARIOS_PUERTO`, `SERVICIO_REPORTES_PUERTO`.

## Ejecución con Docker

```bash
docker compose up -d --build
```

Verificar:

```bash
docker compose ps
curl http://localhost:5073/health
```

Abrir la aplicación en <http://localhost:5267>.

## Ejecución local

```bash
dotnet build FERRETERIA_JOEL.slnx
dotnet run --project ServicioUsuarios
dotnet run --project ServicioVentas
dotnet run --project ServicioReportes
dotnet run --project FERRETERIA_Web
```

Las URLs locales de cada servicio se configuran en
`FERRETERIA_Web/appsettings.json` (`Servicios:*ApiUrl`). `ServicioReportes` lee
la cadena de MySQL desde `ConnectionStrings:MySqlConnection` o desde
`ConnectionStrings__MySqlConnection`.

## Estructura del repositorio

```
FERRETERIA_JOEL.slnx
docker-compose.yml
Scripts/                  Script de inicialización de MySQL
ServicioUsuarios/         API de autenticación y empleados
ServicioVentas/           API de ventas
ServicioReportes/         API de reportes y generación de documentos
FERRETERIA_Web/           UI Razor Pages + documentación
```

## Documentación adicional

- [`FERRETERIA_Web/Documentacion/Reporte_Final_Entregable1_Ventas.md`](FERRETERIA_Web/Documentacion/Reporte_Final_Entregable1_Ventas.md)
- [`FERRETERIA_Web/Documentacion/Informe_SOLID_FerreteriaJoel.md`](FERRETERIA_Web/Documentacion/Informe_SOLID_FerreteriaJoel.md)
- [`FERRETERIA_Web/Documentacion/Guia_Defensa_SOLID.md`](FERRETERIA_Web/Documentacion/Guia_Defensa_SOLID.md)
- [`FERRETERIA_Web/Documentacion/Reporte_Entregable3_Reportes.md`](FERRETERIA_Web/Documentacion/Reporte_Entregable3_Reportes.md)
