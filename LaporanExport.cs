using System.Globalization;
using System.Text;

namespace LogistikApp;

public class LaporanExporter
{
    private const string NamaFileTxt = "laporan_logistik.txt";
    private const string NamaFileCsv = "laporan_paket.csv";

    public bool ExportTxt(LogistikManager manager, out string pesan)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("==========================================");
        sb.AppendLine("   LAPORAN LOGISTIK PENGIRIMAN");
        sb.AppendLine($"   Dibuat pada: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("==========================================");
        sb.AppendLine();
        sb.AppendLine("RINGKASAN");
        sb.AppendLine($"Total paket                 : {manager.DaftarPaket.Count}");
        sb.AppendLine($"Total kurir                 : {manager.DaftarKurir.Count}");
        sb.AppendLine($"Pendapatan terealisasi      : {JenisPengiriman.Rupiah(manager.HitungPendapatanTerealisasi())}");
        sb.AppendLine($"Potensi pendapatan (aktif)  : {JenisPengiriman.Rupiah(manager.HitungPotensiPendapatan())}");
        sb.AppendLine();

        List<IDapatDiringkas> daftarKurir = new();
        foreach (Kurir kurir in manager.DaftarKurir)
            daftarKurir.Add(kurir);

        List<IDapatDiringkas> daftarPaket = new();
        foreach (Paket paket in manager.DaftarPaket)
            daftarPaket.Add(paket);

        TulisBagian(sb, "DAFTAR KURIR", daftarKurir);
        TulisBagian(sb, "DAFTAR PAKET", daftarPaket);

        try
        {
            File.WriteAllText(NamaFileTxt, sb.ToString(), Encoding.UTF8);
            pesan = $"Laporan teks tersimpan di: {Path.GetFullPath(NamaFileTxt)}";
            return true;
        }
        catch (IOException ex)
        {
            pesan = $"Gagal menyimpan laporan teks: {ex.Message}";
            return false;
        }
    }

    public bool ExportCsv(LogistikManager manager, out string pesan)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("ID,Pengirim,Penerima,Alamat,Berat(kg),Layanan,Biaya,Status,Kurir,TanggalDibuat,EstimasiTiba");

        foreach (Paket paket in manager.DaftarPaket)
        {
            string[] kolom =
            {
                Csv(paket.IdPengiriman),
                Csv(paket.NamaPengirim),
                Csv(paket.NamaPenerima),
                Csv(paket.AlamatTujuan),
                paket.Berat.ToString(CultureInfo.InvariantCulture),
                Csv(paket.Jenis.Nama),
                paket.Biaya.ToString(CultureInfo.InvariantCulture),
                Csv(paket.Status.KeTeks()),
                Csv(paket.KurirAktif != null ? paket.KurirAktif.Nama : "-"),
                paket.TanggalDibuat.ToString("yyyy-MM-dd HH:mm"),
                Csv(paket.TeksEstimasiTiba())
            };

            sb.AppendLine(string.Join(",", kolom));
        }

        try
        {
            // UTF-8 dengan BOM agar terbaca benar di Excel
            File.WriteAllText(NamaFileCsv, sb.ToString(), new UTF8Encoding(true));
            pesan = $"Laporan CSV tersimpan di: {Path.GetFullPath(NamaFileCsv)}";
            return true;
        }
        catch (IOException ex)
        {
            pesan = $"Gagal menyimpan laporan CSV: {ex.Message}";
            return false;
        }
    }

    private static void TulisBagian(StringBuilder sb, string judul, List<IDapatDiringkas> daftar)
    {
        sb.AppendLine(judul);
        sb.AppendLine("------------------------------------------");

        if (daftar.Count == 0)
        {
            sb.AppendLine("(kosong)");
        }
        else
        {
            int nomor = 1;
            foreach (IDapatDiringkas item in daftar)
            {
                sb.AppendLine($"{nomor}. {item.BuatRingkasan()}");
                nomor++;
            }
        }

        sb.AppendLine();
    }

    // Membungkus teks yang mengandung koma, tanda kutip, atau baris baru
    private static string Csv(string nilai)
    {
        if (nilai.Contains(',') || nilai.Contains('"') || nilai.Contains('\n'))
            return "\"" + nilai.Replace("\"", "\"\"") + "\"";

        return nilai;
    }
}