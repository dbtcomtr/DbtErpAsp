namespace Deneme_proje.Models
{
    public class YonetimEntities
    {
        public class KullaniciYonetimi
        {
            public int Id { get; set; }
            public string UserNo { get; set; }
            public bool GirisYetkisi { get; set; }
        }

        public class KullaniciListViewModel
        {
            public string UserNo { get; set; }
            public string UserName { get; set; }
            public string LongName { get; set; }
            public string Email { get; set; }
            public bool GirisYetkisi { get; set; }
        }
    }
}
