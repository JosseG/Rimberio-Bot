namespace MyBotConversational.ModelsApi
{
    public class TokenBot
    {

        public long id { get; set; }
        public string token { get; set; }

        public bool expirado { get; set; }
        public Usuario usuario { get; set; }

    }
}
