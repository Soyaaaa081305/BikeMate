using Microsoft.Maui.Controls;

namespace BikeMate.Helpers;

public static class AppTypography
{
    public const double CaptionSize = 11;
    public const double BodySize = 13;
    public const double TitleSize = 18;

    public const string CaptionFont = "PTSansCaption";
    public const string CaptionBoldFont = "PTSansCaptionBold";
    public const string BodyFont = "PublicSans";
    public const string DisplayFont = "Inter";

    public static double SizeFor(double requestedSize)
    {
        if (requestedSize <= CaptionSize)
        {
            return CaptionSize;
        }

        return requestedSize >= 16 ? TitleSize : BodySize;
    }

    public static string FontFor(double requestedSize, FontAttributes attributes = FontAttributes.None)
    {
        var size = SizeFor(requestedSize);
        if (size <= CaptionSize)
        {
            return (attributes & FontAttributes.Bold) != 0 ? CaptionBoldFont : CaptionFont;
        }

        return size >= TitleSize || (attributes & FontAttributes.Bold) != 0 ? DisplayFont : BodyFont;
    }
}
