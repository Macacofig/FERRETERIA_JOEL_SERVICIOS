USE bdferreteria;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD COLUMN UsuarioCreacion INT NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND COLUMN_NAME = 'UsuarioCreacion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD COLUMN FechaCreacion DATETIME NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND COLUMN_NAME = 'FechaCreacion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD COLUMN UsuarioModificacion INT NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND COLUMN_NAME = 'UsuarioModificacion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD COLUMN FechaModificacion DATETIME NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND COLUMN_NAME = 'FechaModificacion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD COLUMN UsuarioBaja INT NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND COLUMN_NAME = 'UsuarioBaja'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD COLUMN FechaBaja DATETIME NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND COLUMN_NAME = 'FechaBaja'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD INDEX IX_empleado_usuario_creacion (UsuarioCreacion)',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND INDEX_NAME = 'IX_empleado_usuario_creacion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD INDEX IX_empleado_usuario_modificacion (UsuarioModificacion)',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND INDEX_NAME = 'IX_empleado_usuario_modificacion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE empleado ADD INDEX IX_empleado_usuario_baja (UsuarioBaja)',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'empleado' AND INDEX_NAME = 'IX_empleado_usuario_baja'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE bitacora_sesion ADD COLUMN Entidad VARCHAR(50) NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bitacora_sesion' AND COLUMN_NAME = 'Entidad'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE bitacora_sesion ADD COLUMN IdRegistroAfectado INT NULL',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bitacora_sesion' AND COLUMN_NAME = 'IdRegistroAfectado'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE bitacora_sesion ADD COLUMN Resultado VARCHAR(20) NOT NULL DEFAULT ''EXITOSO''',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bitacora_sesion' AND COLUMN_NAME = 'Resultado'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE venta ADD COLUMN MetodoPago VARCHAR(20) NOT NULL DEFAULT ''Efectivo''',
        'SELECT 1'
    )
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'venta' AND COLUMN_NAME = 'MetodoPago'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE empleado
SET FechaCreacion = CURRENT_TIMESTAMP
WHERE IdEmpleado > 0 AND FechaCreacion IS NULL;
