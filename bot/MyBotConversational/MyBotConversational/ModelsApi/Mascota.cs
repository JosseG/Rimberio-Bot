using System.Runtime.InteropServices;
using System;

namespace MyBotConversational.ModelsApi
{
    public class Mascota
    {


        public long id { get; set; }
        public Usuario usuario { get; set; }
        public string nombre { get; set; }
        public string especie { get; set; }
        public string raza { get; set; }
        public DateTime fechaNacimiento { get; set; }
        public decimal peso { get; set; }
        public string caracteristicas { get; set; }
        public bool estado { get; set; }

    }
}
