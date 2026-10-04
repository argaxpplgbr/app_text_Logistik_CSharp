namespace LogistikApp;

public static class DataSeeder
{
    public static void Isi(LogistikManager manager)
    {
        // Kurir contoh
        manager.TambahKurir(new Kurir("K01", "Budi Santoso", new Motor()), out _);
        manager.TambahKurir(new Kurir("K02", "Sari Wulandari", new Mobil()), out _);
        manager.TambahKurir(new Kurir("K03", "Joko Pratama", new Truk()), out _);

        // P001: baru dibuat
        manager.TambahPaket(new Paket("P001", "Andi", "Siti", "Jl. Merdeka No. 10, Bandung",
                                      2.5, new PengirimanReguler()), out _);

        // P002: menunggu kurir
        manager.TambahPaket(new Paket("P002", "Rina", "Dewi", "Jl. Asia Afrika No. 5, Bandung",
                                      4, new PengirimanExpress()), out _);
        manager.PerbaruiStatus("P002", StatusPengiriman.MenungguKurir, "Menunggu penjemputan", out _);

        // P003: sudah ditugaskan ke K01 dan dijemput
        manager.TambahPaket(new Paket("P003", "Toko Maju", "Agus", "Jl. Dago No. 88, Bandung",
                                      3, new PengirimanSameDay()), out _);
        manager.PerbaruiStatus("P003", StatusPengiriman.MenungguKurir, "Menunggu penjemputan", out _);
        manager.TugaskanKurir("P003", "K01", out _);
        manager.PerbaruiStatus("P003", StatusPengiriman.DijemputKurir, "Paket diambil dari toko", out _);

        // P004: barang besar via Kargo, menunggu kurir (hanya Truk yang mampu)
        manager.TambahPaket(new Paket("P004", "PT Sentosa", "Gudang Barat", "Jl. Soekarno-Hatta No. 200, Bandung",
                                      120, new PengirimanKargo()), out _);
        manager.PerbaruiStatus("P004", StatusPengiriman.MenungguKurir, "Menunggu penjemputan", out _);
    }
}