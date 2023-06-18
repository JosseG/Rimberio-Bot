using System.Collections.Generic;

namespace MyBotConversational.ModelsApi
{
    public class Usuario
    {

        public long id { get; set; }

        public string username { get; set; }
        public string password { get; set; }
        public string nombre { get; set; }
        public string apellido { get; set; }
        public string email { get; set; }
        public string telefono { get; set; }

        public bool enabled { get; set; }
        public bool accountNonLocked { get; set; }
        public bool accountNonExpired { get; set; }
        public bool credentialsNonExpired { get; set; }

        public List<Authority> authorities { get; set; }


    }
}
