ALTER TABLE empleado
    ADD COLUMN UsuarioCreacion INT NULL,
    ADD COLUMN FechaCreacion DATETIME NULL,
    ADD COLUMN UsuarioModificacion INT NULL,
    ADD COLUMN FechaModificacion DATETIME NULL,
    ADD COLUMN UsuarioBaja INT NULL,
    ADD COLUMN FechaBaja DATETIME NULL,
    ADD INDEX IX_empleado_usuario_creacion (UsuarioCreacion),
    ADD INDEX IX_empleado_usuario_modificacion (UsuarioModificacion),
    ADD INDEX IX_empleado_usuario_baja (UsuarioBaja);

UPDATE empleado
SET FechaCreacion = COALESCE(FechaCreacion, FechaActualizacion, CURRENT_TIMESTAMP)
WHERE FechaCreacion IS NULL;
