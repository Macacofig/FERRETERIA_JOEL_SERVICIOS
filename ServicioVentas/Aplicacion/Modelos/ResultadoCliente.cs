using ServicioVentas.Dominio.Entidades;
using System.Collections.Generic;

namespace ServicioVentas.Aplicacion.Modelos
{
    public class ResultadoCliente
    {
        public bool Exito { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public Cliente? Cliente { get; set; }

        public Dictionary<string, string> Errores { get; set; } = new();
    }
}
