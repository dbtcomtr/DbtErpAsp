namespace Deneme_proje.Models
{
    public class MenuYonetimiViewModel
    {
        public List<KullaniciModel> Kullanicilar { get; set; }
        public List<MenuItemModel> MenuItems { get; set; }
        public int SelectedUserNo { get; set; }  // string yerine int kullanıyoruz
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

        public bool HasUserPermission(int userNo)  // string yerine int parametre
        {
            if (string.IsNullOrEmpty(Yetki)) return false;
            return Yetki.Split(',').Contains(userNo.ToString());
        }
    }

    public class UpdateYetkiModel
    {
        public int MenuId { get; set; }
        public int UserNo { get; set; }
        public bool HasPermission { get; set; }
    }
}
