namespace ServicioReportes.Aplicacion.Modelos.Reportes
{
    /// <summary>Archivo binario listo para devolver por HTTP.</summary>
    public sealed record ArchivoReporte(
        byte[] Contenido,
        string NombreArchivo,
        string TipoContenido)
    {
        public const string TipoPdf = "application/pdf";

        public const string TipoExcel =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    }
}
