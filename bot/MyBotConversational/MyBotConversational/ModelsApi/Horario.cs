using System;

namespace MyBotConversational.ModelsApi
{
    public class Horario
    {

        public long id { get; set; }

        public string diaSemana { get; set; }
        public string horaInicio { get; set; }
        public string horaFin { get; set; }
        public Veterinario veterinario { get; set; }
        public bool estado { get; set; }

    }
}
