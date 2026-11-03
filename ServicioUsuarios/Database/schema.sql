USE bdferreteria;

CREATE TABLE IF NOT EXISTS usuario (
    id BIGINT NOT NULL AUTO_INCREMENT,
    nombre_completo VARCHAR(150) NOT NULL,
    nombre_usuario VARCHAR(100) NOT NULL,
    correo VARCHAR(150) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    rol VARCHAR(20) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT FALSE,
    debe_cambiar_password BOOLEAN NOT NULL DEFAULT TRUE,
    creado_por VARCHAR(100) NULL,
    fecha_creacion DATETIME NOT NULL,
    modificado_por VARCHAR(100) NULL,
    fecha_modificacion DATETIME NULL,
    dado_de_baja_por VARCHAR(100) NULL,
    fecha_baja DATETIME NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_usuario_nombre_usuario (nombre_usuario),
    UNIQUE KEY uk_usuario_correo (correo)
);

CREATE TABLE IF NOT EXISTS token_activacion (
    id BIGINT NOT NULL AUTO_INCREMENT,
    usuario_id BIGINT NOT NULL,
    token_hash CHAR(64) NOT NULL,
    fecha_expiracion DATETIME NOT NULL,
    usado BOOLEAN NOT NULL DEFAULT FALSE,
    creado_por VARCHAR(100) NULL,
    fecha_creacion DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_token_activacion_hash (token_hash),
    CONSTRAINT fk_token_activacion_usuario
        FOREIGN KEY (usuario_id) REFERENCES usuario (id)
);

-- Usuario de prueba exclusivo para desarrollo y pruebas.
-- No utilizar en producción.
INSERT INTO usuario (
    nombre_completo, nombre_usuario, correo, password_hash, rol,
    activo, debe_cambiar_password, creado_por, fecha_creacion
)
SELECT
    'Administrador de Prueba',
    'admin',
    'admin.prueba@ferreteria.local',
    'PBKDF2-SHA256$100000$KJZ4VCscxoP+sNxIBlVLZA==$NV0dPitWllW80MaPc2rxH7znW4EmwrPerrXVIUBxFXs=',
    'Administrador',
    TRUE,
    FALSE,
    'sistema',
    UTC_TIMESTAMP()
WHERE NOT EXISTS (
    SELECT 1 FROM usuario
    WHERE nombre_usuario = 'admin' OR correo = 'admin.prueba@ferreteria.local'
);