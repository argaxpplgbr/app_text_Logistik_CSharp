namespace LogistikApp;

public interface IPengantar
{
    string IdKurir { get; }
    string Nama { get; }

    bool BisaMenerima(Paket paket, out string alasan);
    void AmbilPaket(Paket paket);
    void AntarPaket(Paket paket);
    void BatalkanTugas(Paket paket);
}