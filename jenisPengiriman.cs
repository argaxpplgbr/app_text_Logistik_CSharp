using System.Globalization;

namespace LogistikApp;


public abstract class JenisPengiriman
{
    public abstract string Nama { get; }
    public abstract decimal TarifPerKg { get; }
    public abstract decimal BiayaTetap { get; }

    
    public abstract decimal HitungBiaya(double berat);
    public abstract string EstimasiWaktu(double berat);
    public abstract DateTime HitungTanggalTiba(DateTime mulai, double berat);

    public virtual string CatatanBiaya(double berat)
    {
        return "-";
    }

    
    protected virtual decimal BeratTertagih(double berat)
    {
        return (decimal)Math.Max(1, Math.Ceiling(berat));
    }

  
    protected static DateTime TambahHariKerja(DateTime mulai, int hari)
    {
        DateTime hasil = mulai;
        int sisa = hari;

        while (sisa > 0)
        {
            hasil = hasil.AddDays(1);
            if (hasil.DayOfWeek != DayOfWeek.Saturday && hasil.DayOfWeek != DayOfWeek.Sunday)
                sisa--;
        }

        return hasil;
    }

    public static string Rupiah(decimal nilai)
    {
        return "Rp " + nilai.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
    }

    public void TampilkanRincian(double berat)
    {
        Console.WriteLine($"--- Rincian Biaya: {Nama} ---");
        Console.WriteLine($"Berat paket       : {berat} kg (ditagih {BeratTertagih(berat)} kg)");
        Console.WriteLine($"Tarif per kg      : {Rupiah(TarifPerKg)}");
        Console.WriteLine($"Biaya tetap       : {Rupiah(BiayaTetap)}");
        Console.WriteLine($"Catatan           : {CatatanBiaya(berat)}");
        Console.WriteLine($"TOTAL BIAYA       : {Rupiah(HitungBiaya(berat))}");
        Console.WriteLine($"Estimasi waktu    : {EstimasiWaktu(berat)}");
    }
}