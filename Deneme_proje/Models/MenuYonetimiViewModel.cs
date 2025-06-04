namespace Deneme_proje.Models
{
    public class MenuYonetimiViewModel
    {
        public List<KullaniciModel> Kullanicilar { get; set; }
        public List<MenuItemModel> MenuItems { get; set; }
        public Dictionary<string, List<MenuItemModel>> KategoriliMenuler { get; set; } // YENİ
        public int SelectedUserNo { get; set; }
    }

    public class KullaniciModel
    {
        public int UserNo { get; set; }
        public string UserName { get; set; }
    }

    public class MenuItemModel
    {
        public int Id { get; set; }
        public string ControllerAdi { get; set; }
        public string ActionAdi { get; set; }
        public string MenuAdi { get; set; }
        public string Yetki { get; set; }
        public int SiraNo { get; set; }
        public int? ParentId { get; set; }
        public string Icon { get; set; }
        public string Kategori { get; set; } // YENİ ALAN
        public string MenuBaslik { get; set; } // YENİ ALAN

        public bool HasUserPermission(int userNo)
        {
            if (string.IsNullOrEmpty(Yetki)) return false;
            return Yetki.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(userNo.ToString());
        }
    }

    public class UpdateYetkiModel
    {
        public int MenuId { get; set; }
        public int UserNo { get; set; }
        public bool HasPermission { get; set; }
    }

    public class MenuYonetimi
    {
        public int Id { get; set; }
        public string MenuElemanId { get; set; }
        public string MenuElemanYolu { get; set; }
        public string MenuElemanAdi { get; set; }
        public bool Gorunur { get; set; }
        public int KullaniciRolId { get; set; }
    }
}