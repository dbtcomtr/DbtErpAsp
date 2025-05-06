namespace Deneme_proje.Models
{
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
