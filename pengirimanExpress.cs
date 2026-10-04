namespace LogistikApp;

public class PengirimanExpress : JenisPengiriman
{
    public override string Nama => "Express";
    public override decimal TarifPerKg => 15000m;
    public override decimal BiayaTetap => 10000m;

    public override decimal HitungBiaya(double berat)
    {
        return BiayaTetap + (TarifPerKg * BeratTertagih(berat));
    }

    public override string EstimasiWaktu(double berat)
    {
        return berat <= 10 ? "1 - 2 hari kerja" : "2 - 3 hari kerja";
    }

    public override DateTime HitungTanggalTiba(DateTime mulai, double berat)
    {
        return TambahHariKerja(mulai, berat <= 10 ? 2 : 3);
    }

    public override string CatatanBiaya(double berat)
    {
        return "Biaya prioritas sudah termasuk";
    }
}