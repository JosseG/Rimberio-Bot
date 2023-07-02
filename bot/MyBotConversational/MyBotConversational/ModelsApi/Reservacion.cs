using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;

namespace MyBotConversational.ModelsApi
{
    public class Reservacion
    {
        public long id { get; set; }
        public Mascota mascota { get; set; }
        public Veterinario veterinario { get; set; }

        public string fecha { get; set; }

        public string hora { get; set; }

        public Boolean estado { get; set; }

    }
}
