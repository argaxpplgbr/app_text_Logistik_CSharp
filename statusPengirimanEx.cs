namespace LogistikApp;

public static class StatusPengirimanExtensions
{
    public static string KeTeks(this StatusPengiriman status)
    {
        return status switch
        {
            StatusPengiriman.PaketDibuat => "Paket dibuat",
            StatusPengiriman.MenungguKurir => "Menunggu kurir",
            StatusPengiriman.DijemputKurir => "Dijemput kurir",
            StatusPengiriman.DalamPerjalanan => "Dalam perjalanan",
            StatusPengiriman.SampaiDiPusatSortir => "Sampai di pusat sortir",
            StatusPengiriman.SedangDiantar => "Sedang diantar",
            StatusPengiriman.BerhasilDikirim => "Berhasil dikirim",
            StatusPengiriman.Dibatalkan => "Dibatalkan",
            _ => status.ToString()
        };
    }
}