namespace LogistikApp;

public class PengirimanKargo : JenisPengiriman
{
    private const decimal MinimalBeratTagih = 10m;
    private const double BatasBeratNormal = 50;

    public override string Nama => "Kargo";
    public override decimal TarifPerKg => 5000m;
    public override decimal BiayaTetap => 25000m;

    // Kargo punya minimal berat tagihan 10 kg
    protected override decimal BeratTertagih(double berat)
    {
        return Math.Max(MinimalBeratTagih, base.BeratTertagih(berat));
    }

    public override decimal HitungBiaya(double berat)
    {
        return BiayaTetap + (TarifPerKg * BeratTertagih(berat));
    }

    public override string EstimasiWaktu(double berat)
    {
        return berat <= BatasBeratNormal ? "5 - 10 hari kerja" : "7 - 14 hari kerja";
    }

    public override DateTime HitungTanggalTiba(DateTime mulai, double berat)
    {
        return TambahHariKerja(mulai, berat <= BatasBeratNormal ? 10 : 14);
    }

    public override string CatatanBiaya(double berat)
    {
        return "Minimal tagihan 10 kg (cocok untuk barang besar)";
    }
}