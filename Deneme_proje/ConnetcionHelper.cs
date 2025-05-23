using Microsoft.Extensions.Configuration;

namespace Deneme_proje.Helpers
{
    public static class ConnectionHelper
    {
        private static IConfiguration _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GetConnectionString(string connectionName)
        {
            return _configuration.GetConnectionString(connectionName);
        }
    }
}