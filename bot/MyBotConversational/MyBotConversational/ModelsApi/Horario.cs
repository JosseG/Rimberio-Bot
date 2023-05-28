using System;

namespace MyBotConversational.ModelsApi
{
    public class Horario
    {

        public long codigo { get; set; }
        public DateTime fecha { get; set; }
        public Medico medico { get; set; }

    }
}
