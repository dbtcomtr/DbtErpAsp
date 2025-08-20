using System.ComponentModel.DataAnnotations;

namespace Deneme_proje.Models
{
    public class YonetimEntities
    {
        public class KullaniciYonetimi
        {
            public int Id { get; set; }
            public string UserNo { get; set; }
            public bool GirisYetkisi { get; set; }
            public string IsMerkezleri { get; set; } // Virgülle ayrılmış iş merkezi kodları
        }

        public class KullaniciListViewModel
        {
            public string UserNo { get; set; }
            public string UserName { get; set; }
            public string LongName { get; set; }
            public string Email { get; set; }
            public bool GirisYetkisi { get; set; }
            public string IsMerkezleri { get; set; } // Virgülle ayrılmış iş merkezi kodları

            // IsMerkezleri string'ini liste olarak döndüren property
            public List<string> IsMerkeziListesi =>
                string.IsNullOrEmpty(IsMerkezleri) ?
                new List<string>() :
                IsMerkezleri.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(x => x.Trim())
                           .Where(x => !string.IsNullOrEmpty(x))
                           .ToList();
        }

        // İş merkezi modeli
        public class IsMerkezi
        {
            public string IsM_Kodu { get; set; }
            public string IsM_Aciklama { get; set; }
            public bool IsSelected { get; set; } = false;
        }

        // İş merkezi yetkilendirme view modeli
        public class KullaniciIsMerkeziYetkiViewModel
        {
            public string UserNo { get; set; }
            public string UserName { get; set; }
            public string LongName { get; set; }
            public List<IsMerkezi> TumIsMerkezleri { get; set; } = new List<IsMerkezi>();
            public List<string> SeciliIsMerkezleri { get; set; } = new List<string>();
        }

        // İş merkezi güncelleme modeli
        public class IsMerkeziUpdateModel
        {
            [Required]
            public string UserNo { get; set; }
            public List<string> IsMerkezleri { get; set; } = new List<string>();
        }

        // Hata view modeli
        public class ErrorViewModel
        {
            public string RequestId { get; set; }
            public string ErrorMessage { get; set; }
            public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        }
    }
}