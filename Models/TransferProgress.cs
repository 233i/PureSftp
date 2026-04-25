namespace PureSFTP.Models;

public sealed class TransferProgress
{
    public TransferProgress(long bytesTransferred, long totalBytes)
    {
        BytesTransferred = bytesTransferred;
        TotalBytes = totalBytes;
    }

    public long BytesTransferred { get; }

    public long TotalBytes { get; }

    public double Percent =>
        TotalBytes <= 0
            ? 0
            : BytesTransferred * 100d / TotalBytes;
}
