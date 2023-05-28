using System.Collections.Generic;

namespace MyBotConversational.ModelsApi
{
    public class Medico
    {

        long codigo { get; set; }
        string nombre { get; set; }
        Servicio servicio { get; set; }

    }
}
