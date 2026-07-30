namespace PublicApi.Features;

public static class Common
{
    public sealed record PdfResposeData(byte[] PdfBytes, string FileName);
}
