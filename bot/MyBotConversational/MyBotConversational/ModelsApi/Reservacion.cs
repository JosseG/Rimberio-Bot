using System.Collections.Generic;

namespace MyBotConversational.ModelsApi
{
    public class Reservacion
    {
        public long codigo { get; set; } 
        public bool estado { get; set; }
        public Mascota mascota { get; set; }
        public Horario horario { get; set; }

    }
}
