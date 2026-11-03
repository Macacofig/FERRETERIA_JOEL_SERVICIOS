-- ============================================================
-- FERRETERÍA JOEL — ESQUEMA Y DATOS SEMILLA (script único)
-- Base de datos: bdferreteria
--
-- Este es el ÚNICO script de base de datos del proyecto y es el que monta
-- Docker: docker-compose.yml lo copia en
-- /docker-entrypoint-initdb.d/01-init.sql, de modo que MySQL lo ejecuta solo
-- la primera vez que se crea el volumen.
--
-- Contiene TODO lo que necesita la aplicación:
--   - creación de la base y del juego de tablas final (catálogo, clientes,
--     empleados, ventas, histórico de precios y bitácora de sesiones);
--   - los índices únicos (correo del empleado, código de categoría, razón
--     social del proveedor, código de producto y CI del cliente);
--   - los datos semilla (10 empleados, 10 categorías, 10 marcas, 10
--     proveedores, 10 productos y 5 clientes).
--
-- Es el esquema canónico: las columnas coinciden exactamente con las que
-- leen y escriben los repositorios de la aplicación. Si se modifica una
-- columna, hay que modificarla también en el código C#.
--
-- Ejecutar UNA sola vez sobre una base vacía.
-- ============================================================

CREATE DATABASE IF NOT EXISTS bdferreteria
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE bdferreteria;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS historico_precio;
DROP TABLE IF EXISTS detalle_venta;
DROP TABLE IF EXISTS venta;
DROP TABLE IF EXISTS cliente;
DROP TABLE IF EXISTS producto;
DROP TABLE IF EXISTS proveedor;
DROP TABLE IF EXISTS categoria;
DROP TABLE IF EXISTS marca;
DROP TABLE IF EXISTS empleado;
DROP TABLE IF EXISTS bitacora_sesion;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- EMPLEADO
-- ============================================================
CREATE TABLE empleado
(
    IdEmpleado            INT          NOT NULL AUTO_INCREMENT,
    Nombre                VARCHAR(100) NOT NULL,
    Apellido              VARCHAR(100) NOT NULL,
    NombreUsuario         VARCHAR(100) NULL DEFAULT NULL,
    Rol                   VARCHAR(50)  NOT NULL,
    Telefono              VARCHAR(20)  NOT NULL,
    CorreoElectronico     VARCHAR(150) NULL DEFAULT NULL,
    Contrasena            VARCHAR(255) NULL DEFAULT NULL,
    TokenActivacion       VARCHAR(128) NULL DEFAULT NULL,
    TokenExpira           DATETIME     NULL DEFAULT NULL,
    TokenUsado            TINYINT(1)   NOT NULL DEFAULT 0,
    Estado                TINYINT      NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME     NULL DEFAULT NULL,
    IdEmpleadoResponsable INT          NULL DEFAULT NULL,
    PRIMARY KEY (IdEmpleado),
    UNIQUE KEY UNIQUE_EMPLEADO_CORREO (CorreoElectronico),
    UNIQUE KEY UNIQUE_EMPLEADO_USUARIO (NombreUsuario),
    KEY IDX_EMPLEADO_TOKEN (TokenActivacion),
    CONSTRAINT FK_empleado_responsable
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- MARCA
-- ============================================================
CREATE TABLE marca
(
    IdMarca               INT           NOT NULL AUTO_INCREMENT,
    Nombre                VARCHAR(100)  NOT NULL,
    Descripcion           VARCHAR(255)  NULL DEFAULT NULL,
    PaisOrigen            VARCHAR(100)  NULL DEFAULT NULL,
    Calidad               VARCHAR(50)   NULL DEFAULT NULL,
    Estado                TINYINT       NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME     NULL DEFAULT NULL,
    IdEmpleadoResponsable INT           NOT NULL,
    PRIMARY KEY (IdMarca),
    CONSTRAINT FK_marca_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- CATEGORIA
-- ============================================================
CREATE TABLE categoria
(
    IdCategoria           INT            NOT NULL AUTO_INCREMENT,
    Codigo                VARCHAR(50)    NOT NULL,
    Nombre                VARCHAR(100)   NOT NULL,
    Descripcion           VARCHAR(255)   NULL DEFAULT NULL,
    PorcentajeGanancia    DECIMAL(10,2)  NOT NULL DEFAULT 0,
    Estado                TINYINT        NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME       NULL DEFAULT NULL,
    IdEmpleadoResponsable INT            NOT NULL,
    PRIMARY KEY (IdCategoria),
    UNIQUE KEY UNIQUE_CATEGORIA_CODIGO (Codigo),
    CONSTRAINT FK_categoria_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- PROVEEDOR
-- ============================================================
CREATE TABLE proveedor
(
    IdProveedor           INT           NOT NULL AUTO_INCREMENT,
    RazonSocial           VARCHAR(150)  NOT NULL,
    NombreComercial       VARCHAR(150)  NOT NULL,
    NombreContacto        VARCHAR(150)  NOT NULL,
    Telefono              VARCHAR(20)   NOT NULL,
    CorreoElectronico     VARCHAR(150)  NULL DEFAULT NULL,
    Direccion             VARCHAR(255)  NULL DEFAULT NULL,
    Estado                TINYINT       NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME     NULL DEFAULT NULL,
    IdEmpleadoResponsable INT           NOT NULL,
    PRIMARY KEY (IdProveedor),
    UNIQUE KEY UNIQUE_PROVEEDOR_RAZON (RazonSocial),
    CONSTRAINT FK_proveedor_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- PRODUCTO
-- ============================================================
CREATE TABLE producto
(
    IdProducto            INT           NOT NULL AUTO_INCREMENT,
    IdCategoria           INT           NOT NULL,
    Codigo                VARCHAR(50)   NOT NULL,
    Nombre                VARCHAR(100)  NOT NULL,
    Descripcion           VARCHAR(255)  NULL DEFAULT NULL,
    Marca                 INT           NOT NULL,
    UnidadMedida          VARCHAR(50)   NOT NULL,
    PrecioVenta           DECIMAL(12,2) NOT NULL DEFAULT 0,
    CantidadStock         INT           NOT NULL DEFAULT 0,
    Estado                TINYINT       NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME     NULL DEFAULT NULL,
    IdEmpleadoResponsable INT           NOT NULL,
    PRIMARY KEY (IdProducto),
    UNIQUE KEY UNIQUE_PRODUCTO_CODIGO (Codigo),
    CONSTRAINT FK_producto_categoria
        FOREIGN KEY (IdCategoria) REFERENCES categoria (IdCategoria),
    CONSTRAINT FK_producto_marca
        FOREIGN KEY (Marca) REFERENCES marca (IdMarca),
    CONSTRAINT FK_producto_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- CLIENTE (con CI desde el inicio, SIN credenciales, SIN auto-referencia)
-- ============================================================
CREATE TABLE cliente
(
    IdCliente             INT           NOT NULL AUTO_INCREMENT,
    Ci                    VARCHAR(15)   NULL DEFAULT NULL,
    Nombre                VARCHAR(100)  NOT NULL,
    Apellido              VARCHAR(100)  NOT NULL,
    Estado                TINYINT       NOT NULL DEFAULT 1,
    FechaRegistro         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME     NULL DEFAULT NULL,
    IdEmpleadoResponsable INT           NULL DEFAULT NULL,
    PRIMARY KEY (IdCliente),
    UNIQUE KEY UNIQUE_CLIENTE_CI (Ci),
    CONSTRAINT FK_cliente_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- VENTA (con campos finales: estado y auditoría)
-- ============================================================
CREATE TABLE venta
(
    IdVenta               INT            NOT NULL AUTO_INCREMENT,
    CodigoVenta           VARCHAR(50)    NULL DEFAULT NULL,
    EstadoVenta           VARCHAR(20)    NOT NULL DEFAULT 'Registrada',
    IdCliente             INT            NOT NULL,
    MetodoPago            VARCHAR(50)    NOT NULL DEFAULT 'Efectivo',
    EstadoPago            VARCHAR(20)    NOT NULL DEFAULT 'Pendiente',
    MontoTotal            DECIMAL(12,2)  NOT NULL DEFAULT 0,
    FechaRegistro         DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaActualizacion    DATETIME       NULL DEFAULT NULL,
    FechaAnulacion        DATETIME       NULL DEFAULT NULL,
    MotivoAnulacion       VARCHAR(255)   NULL DEFAULT NULL,
    IdEmpleadoAnulacion   INT            NULL DEFAULT NULL,
    IdEmpleadoResponsable INT            NOT NULL,
    PRIMARY KEY (IdVenta),
    CONSTRAINT FK_venta_cliente
        FOREIGN KEY (IdCliente) REFERENCES cliente (IdCliente),
    CONSTRAINT FK_venta_empleado_anulacion
        FOREIGN KEY (IdEmpleadoAnulacion) REFERENCES empleado (IdEmpleado),
    CONSTRAINT FK_venta_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- DETALLE_VENTA
-- ============================================================
CREATE TABLE detalle_venta
(
    IdDetalleVenta        INT           NOT NULL AUTO_INCREMENT,
    IdVenta               INT           NOT NULL,
    IdProducto            INT           NOT NULL,
    Cantidad              INT           NOT NULL,
    PrecioUnitario        DECIMAL(12,2) NOT NULL,
    TotalProducto         DECIMAL(12,2) NOT NULL,
    FechaRegistro         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdEmpleadoResponsable INT           NOT NULL,
    PRIMARY KEY (IdDetalleVenta),
    CONSTRAINT FK_detalle_venta
        FOREIGN KEY (IdVenta) REFERENCES venta (IdVenta),
    CONSTRAINT FK_detalle_producto
        FOREIGN KEY (IdProducto) REFERENCES producto (IdProducto),
    CONSTRAINT FK_detalle_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- HISTORICO_PRECIO
-- ============================================================
CREATE TABLE historico_precio
(
    IdHistoricoPrecio     INT           NOT NULL AUTO_INCREMENT,
    IdProducto            INT           NOT NULL,
    Precio                DECIMAL(12,2) NOT NULL,
    FechaInicioVigencia   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaFinVigencia      DATETIME      NULL DEFAULT NULL,
    MotivoCambio          VARCHAR(255)  NOT NULL DEFAULT '',
    FechaRegistro         DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdEmpleadoResponsable INT           NOT NULL,
    PRIMARY KEY (IdHistoricoPrecio),
    CONSTRAINT FK_historico_producto
        FOREIGN KEY (IdProducto) REFERENCES producto (IdProducto),
    CONSTRAINT FK_historico_empleado
        FOREIGN KEY (IdEmpleadoResponsable) REFERENCES empleado (IdEmpleado)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- BITACORA_SESION (login / logout)
-- ============================================================
CREATE TABLE bitacora_sesion
(
    IdBitacora    INT          NOT NULL AUTO_INCREMENT,
    Accion        VARCHAR(20)  NOT NULL COMMENT 'login | logout',
    IdUsuario     INT          NULL DEFAULT NULL,
    NombreUsuario VARCHAR(150) NULL DEFAULT NULL,
    TipoUsuario   VARCHAR(50)  NOT NULL COMMENT 'Administrador | EmpleadoVenta',
    FechaHora     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (IdBitacora),
    KEY IDX_BITACORA_FECHA (FechaHora),
    KEY IDX_BITACORA_USUARIO (TipoUsuario, IdUsuario)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ============================================================
-- DATOS SEMILLA
-- ============================================================

-- EMPLEADOS (10)
-- Contraseñas: Argon2id
-- admin123 / clave123
INSERT INTO empleado
    (Nombre, Apellido, NombreUsuario, Rol, Telefono, CorreoElectronico, Contrasena, Estado, IdEmpleadoResponsable)
VALUES
    ('Joel',   'Ríos',      'jrios',          'Administrador', '70000001', 'joel.rios@ferreteria.com',    '$argon2id$v=19$m=19456,t=2,p=1$Z+Kb/5STembq6DWHh6ZgBg$vSM1I7ha07sC1lViQivu6mQFHo52FiCSl5zSYMFcfqI',   1, NULL),
    ('María',  'López',     'mlopez',         'EmpleadoVenta', '70000002', 'maria.lopez@ferreteria.com',    '$argon2id$v=19$m=19456,t=2,p=1$432CJ2xhzoutD3YceF5mfQ$BGAYPqa6XVk6osyHgnFTeg7pB5NL8yAK05UhDHHlRm0', 1, 1),
    ('Carlos', 'Mendoza',   'cmendoza',       'EmpleadoVenta', '70000003', 'carlos.mendoza@ferreteria.com', '$argon2id$v=19$m=19456,t=2,p=1$UxDIBBPv5brarthGWQNkRQ$73iY2wXaUNsrAlHpXgImy3ZrigjX0q5UIPATOirSJgY', 1, 1),
    ('Ana',    'Gutiérrez', 'agutierrez',     'EmpleadoVenta', '70000004', 'ana.gutierrez@ferreteria.com',  '$argon2id$v=19$m=19456,t=2,p=1$U8EUBa0bu4tDk3f82hCdHg$D4YF8Aq9qprVM1q3lPNC9FbXdq09lQkw2dK1pnridQY', 1, 1),
    ('Luis',   'Fernández', 'lfernandez',     'EmpleadoVenta', '70000005', 'luis.fernandez@ferreteria.com', '$argon2id$v=19$m=19456,t=2,p=1$mXiLh5CNwb1XmsPoZlCKYg$vqF5qWziDQV+EoyFtBBK9omnCnJTM6ywS78DephbAwM', 1, 1),
    ('Sofía',  'Ramírez',   'sramirez',       'EmpleadoVenta', '70000006', 'sofia.ramirez@ferreteria.com',  '$argon2id$v=19$m=19456,t=2,p=1$UKLS7lcxWKbEWJvAacTqRw$b65cck9cbwNYbvCFTLly9nQfAKimKKV/PIEyGj+fvvc', 1, 1),
    ('Diego',  'Torres',    'dtorres',        'EmpleadoVenta', '70000007', 'diego.torres@ferreteria.com',   '$argon2id$v=19$m=19456,t=2,p=1$Rrba/l4K/4cRwn61PvA9gw$4IRRCq2JTCzsREoDcDDPqB5DczR+VrzRol4/WGyeTU4', 1, 1),
    ('Lucía',  'Vargas',    'lvargas',        'EmpleadoVenta', '70000008', 'lucia.vargas@ferreteria.com',   '$argon2id$v=19$m=19456,t=2,p=1$0Fns0IHrq/Kd6krlFhsQIQ$yjXH1tZbCRCs8ZbKwoybMf4UEkZVTc/EfZGUX/yFnDo', 1, 1),
    ('Pedro',  'Castillo',  'pcastillo',      'EmpleadoVenta', '70000009', 'pedro.castillo@ferreteria.com', '$argon2id$v=19$m=19456,t=2,p=1$DkC+fWxOlRk1/X6aGj7e9A$a34WBFccYECQ3Ka+UwVczdl8Rn5zku1/kXvK+Yjpdsc', 1, 1),
    ('Elena',  'Rojas',     'erojas',         'EmpleadoVenta', '70000010', 'elena.rojas@ferreteria.com',    '$argon2id$v=19$m=19456,t=2,p=1$6lLxKhg6camSJXMOp3SN9A$vkifcdnD0iQr6VnYP1kF05W/7N/4AcCXfrd7Olubf0o', 1, 1);

-- CLIENTES (5) - con CI desde el inicio, sin teléfono
INSERT INTO cliente
    (Ci, Nombre, Apellido, Estado, IdEmpleadoResponsable)
VALUES
    ('1234567', 'Juan',   'Pérez',  1, 1),
    ('2345678', 'María',  'Quispe', 1, 1),
    ('3456789', 'Carlos', 'Ticona', 1, 1),
    ('4567890', 'Ana',    'Mamani', 1, 1),
    ('5678901', 'Luis',   'Choque', 1, 1);

-- MARCAS (10)
INSERT INTO marca
    (Nombre, Descripcion, PaisOrigen, Calidad, Estado, IdEmpleadoResponsable)
VALUES
    ('Bosch',       'Herramientas eléctricas',            'Alemania',    'Alta',  1, 1),
    ('Stanley',     'Herramientas manuales',              'Estados Unidos', 'Alta', 1, 1),
    ('Truper',      'Herramientas y ferreteria general',  'México',      'Media', 1, 1),
    ('DeWalt',      'Herramientas eléctricas profesionales','Estados Unidos','Alta', 1, 1),
    ('Makita',      'Herramientas eléctricas',            'Japón',       'Alta',  1, 1),
    ('Milwaukee',   'Herramientas eléctricas',            'Estados Unidos','Alta', 1, 1),
    ('M',           'Seguridad industrial',               'Estados Unidos','Alta', 1, 1),
    ('Sika',        'Adhesivos y sellantes',              'Suiza',       'Alta',  1, 1),
    ('Fischer',     'Fijaciones y anclajes',              'Alemania',    'Alta', 1, 1),
    ('Cemex',       'Materiales de construcción',         'México',      'Alta', 1, 1);

-- CATEGORÍAS (10)
INSERT INTO categoria
    (Codigo, Nombre, Descripcion, PorcentajeGanancia, Estado, IdEmpleadoResponsable)
VALUES
    ('HER-01',  'Herramientas Manuales',   'Martillos desarmadores y llaves',    25.00, 1, 1),
    ('HER-02',  'Herramientas Eléctricas', 'Taladros esmeriles y sierras',       30.00, 1, 1),
    ('PLM-01',  'Plomería',                'Tubería conexiones y grifería',      20.00, 1, 1),
    ('ELE-01',  'Electricidad',            'Cables interruptores y contactos',   22.00, 1, 1),
    ('PIN-01',  'Pinturas',                'Pinturas brochas y rodillos',        35.00, 1, 1),
    ('FIJ-01',  'Fijaciones y Tornillería','Tornillos taquetes y anclajes',      40.00, 1, 1),
    ('SEG-01',  'Seguridad Industrial',    'Cascos guantes y respiradores',      28.00, 1, 1),
    ('JAR-01',  'Jardinería',              'Herramientas y accesorios de jardín',30.00, 1, 1),
    ('CON-01',  'Construcción',            'Cemento varilla y agregados',        18.00, 1, 1),
    ('ADP-01',  'Adhesivos y Sellantes',   'Silicona pegamento y selladores',    32.00, 1, 1);

-- PRODUCTOS (10)
INSERT INTO producto
    (IdCategoria, Codigo, Nombre, Descripcion, Marca, UnidadMedida, PrecioVenta, CantidadStock, Estado, IdEmpleadoResponsable)
VALUES
    (1,  'PROD-001', 'Martillo de una oz',        'Mango de fibra de vidrio',        3, 'Pieza', 185.50,  40, 1, 1),
    (1,  'PROD-002', 'Juego de desarmadores pzas', 'Puntas plana y estrella',         2, 'Juego', 320.00,  25, 1, 1),
    (2,  'PROD-003', 'Taladro percutor W',        'Portabrocas pulgada',             1, 'Pieza', 1450.00, 15, 1, 1),
    (2,  'PROD-004', 'Esmeriladora angular',  'Velocidad variable',              4, 'Pieza', 1680.00, 12, 1, 1),
    (2,  'PROD-005', 'Sierra circular',       'Incluye disco de corte',          5, 'Pieza', 2100.00, 10, 1, 1),
    (6,  'PROD-006', 'Ancla expansiva',         'Caja con piezas',              9, 'Caja',  420.00,  30, 1, 1),
    (10, 'PROD-007', 'Silicona transparente ml',  'Sellador multipropósito',         8, 'Pieza', 95.00,   60, 1, 1),
    (7,  'PROD-008', 'Respirador media cara',        'Incluye dos filtros',             7, 'Pieza', 540.00,  20, 1, 1),
    (9,  'PROD-009', 'Cemento gris kg',           'Uso general en construcción',    10, 'Saco',  280.00,  50, 1, 1),
    (2,  'PROD-010', 'Rotomartillo SDS Plus W',   'Empuñadura antivibración',        6, 'Pieza', 2650.00,  8, 1, 1);

-- PROVEEDORES (10)
INSERT INTO proveedor
    (RazonSocial, NombreComercial, NombreContacto, Telefono, CorreoElectronico, Direccion, Estado, IdEmpleadoResponsable)
VALUES
    ('Distribuidora Ferretera del Norte SA de CV', 'FerreNorte',   'Jorge Salinas',  '81000001', 'ventas@ferrenorte.com',   'Av Industrial Monterrey', 1, 1),
    ('Herramientas Profesionales del Bajío',           'ProfesBajio',  'Rosa Herrera',   '81000002', 'contacto@profesbajio.com','Blvd del Trabajo León',   1, 1),
    ('Suministros Eléctricos MX',                      'ElectroSum',   'Hugo Marrero',   '81000003', 'info@electrosum.com',     'Calle Voltios CDMX',         1, 1),
    ('Plomería y Tubería Nacional',                    'PlomNal',      'Sara Duarte',    '81000004', 'ventas@plomnal.com',      'Av Canales Guadalajara',   1, 1),
    ('Pinturas y Acabados del Sur',                    'PintuSur',     'Mario Cano',     '81000005', 'pedidos@pintusur.com',    'Calle Color Puebla',         1, 1),
    ('Fijaciones Técnicas del Centro',                 'FijaTec',      'Nadia Ríos',     '81000006', 'contacto@fijatec.com',    'Av Tornillos Querétaro',  1, 1),
    ('Seguridad Industrial Integral',                  'SeguridadInt', 'Raúl Pineda',    '81000007', 'ventas@seguridadint.com', 'Blvd Protección Tijuana',  1, 1),
    ('Adhesivos y Químicos del Golfo',                 'AdhiGolfo',    'Irene Solís',    '81000008', 'info@adhigolfo.com',      'Zona Portuaria Veracruz',   1, 1),
    ('Materiales de Construcción del Pacífico',        'ConsPacifico', 'Tomás Lira',     '81000009', 'ventas@conspacifico.com', 'Av Concreto Mazatlán',    1, 1),
    ('Importadora de Herramientas Globales',           'ImportGlobal', 'Karla Mena',     '81000010', 'contacto@importglobal.com','Calle Comercio Mérida',     1, 1);
