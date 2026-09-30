using System.Text;

namespace Client.Data.BMD.Tooltip
{
    internal static class MuTooltipEncoding
    {
        private static readonly Encoding _encoding;

        static MuTooltipEncoding()
        {
            // Required by .NET for legacy Windows code pages such as CP949.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Korean Windows encoding used by the official Webzen client data.
            _encoding = Encoding.GetEncoding(949);
        }

        public static string DecodeNullTerminated(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            int length = Array.IndexOf(bytes, (byte)0);

            if (length < 0)
            {
                length = bytes.Length;
            }

            return _encoding
                .GetString(bytes, 0, length)
                .Trim();
        }
    }
}