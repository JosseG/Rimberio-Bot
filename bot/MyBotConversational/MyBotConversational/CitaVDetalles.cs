using MyBotConversational.ModelsApi;

namespace MyBotConversational
{
    public class CitaVDetalles
    {
        public long idUsuario { get; set; }
        public string idcita { get; set; }

        public string username { get; set; }
        public long tipoInteraccion { get; set; }
        public Mascota mascota { get; set; }
        public string temporalEmail { get; set; }
    }
}
