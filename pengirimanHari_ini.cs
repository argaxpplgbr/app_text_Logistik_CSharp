namespace LogistikApp;

public class PengirimanSameDay : JenisPengiriman
{
    private const double BatasBeratNormal = 5;
    private const decimal PersenSurcharge = 0.20m;

    public override string Nama => "Same Day";
    public override decimal TarifPerKg => 25000m;
    public override decimal BiayaTetap => 20000m;

    public override decimal HitungBiaya(double berat)
    {
        decimal total = BiayaTetap + (TarifPerKg * BeratTertagih(berat));

        if (berat > BatasBeratNormal)
        {
            total += total * PersenSurcharge;
        }

        return total;
    }

    public override string EstimasiWaktu(double berat)
    {
        return berat <= BatasBeratNormal
            ? "Maksimal 8 jam (tiba di hari yang sama)"
            : "Maksimal 10 jam (tiba di hari yang sama)";
    }

    public override DateTime HitungTanggalTiba(DateTime mulai, double berat)
    {
        return mulai.AddHours(berat <= BatasBeratNormal ? 8 : 10);
    }

    public override string CatatanBiaya(double berat)
    {
        return berat > BatasBeratNormal
            ? "Surcharge 20% (berat di atas 5 kg)"
            : "Tidak ada surcharge";
    }
}