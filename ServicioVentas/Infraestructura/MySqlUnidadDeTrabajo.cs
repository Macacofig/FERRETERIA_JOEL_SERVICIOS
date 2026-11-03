using ServicioVentas.Dominio.Puertos;
using ServicioVentas.Infraestructura.Conexion;

namespace ServicioVentas.Infraestructura
{
    public sealed class MySqlUnidadDeTrabajo : IUnidadDeTrabajo
    {
        private readonly IConexionTransaccional _conexionTransaccional;

        public MySqlUnidadDeTrabajo(IConexionTransaccional conexionTransaccional)
        {
            _conexionTransaccional = conexionTransaccional;
        }

        public Task IniciarAsync()
        {
            return _conexionTransaccional.IniciarTransaccionAsync();
        }

        public Task ConfirmarAsync()
        {
            return _conexionTransaccional.ConfirmarAsync();
        }

        public Task RevertirAsync()
        {
            return _conexionTransaccional.RevertirAsync();
        }
    }
}
