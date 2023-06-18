using System.Runtime.InteropServices;
using System;

namespace MyBotConversational.ModelsApi
{
    public class Veterinario
    {
        public long id { get; set; }
        public string nombres { get; set; }
        public string apellidoPaterno { get; set; }
        public string apellidoMaterno { get; set; }
        public Especialidad especialidad { get; set; }
        public bool estado { get; set; } 
    }
}
