namespace Refline.Business.Identity;

public static class ApiIdentityIdMapper
{
    private static readonly byte[] Marker = "REFLINE1"u8.ToArray();

    public static Guid ToLocalGuid(long serverId)
    {
        if (serverId <= 0)
        {
            return Guid.Empty;
        }

        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, sizeof(long)), serverId);
        Marker.CopyTo(bytes.AsSpan(sizeof(long), Marker.Length));

        return new Guid(bytes);
    }

    public static long ToServerId(Guid localId)
    {
        if (localId == Guid.Empty)
        {
            return 0;
        }

        return BitConverter.ToInt64(localId.ToByteArray(), 0);
    }
}
