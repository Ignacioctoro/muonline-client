using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Client.Data.Texture
{
    public class OZPReader : BaseReader<TextureData>
    {
        public const int MAX_WIDTH = 1024;
        public const int MAX_HEIGHT = 1024;

        protected override TextureData Read(byte[] buffer)
        {
            // PNG signature:
            // 89 50 4E 47 0D 0A 1A 0A
            if (buffer == null || buffer.Length < 8)
            {
                throw new ApplicationException("Invalid PNG/OZP file.");
            }

            bool isPng =
                buffer[0] == 137 &&
                buffer[1] == 80 &&
                buffer[2] == 78 &&
                buffer[3] == 71 &&
                buffer[4] == 13 &&
                buffer[5] == 10 &&
                buffer[6] == 26 &&
                buffer[7] == 10;

            if (!isPng)
            {
                throw new ApplicationException("Invalid PNG/OZP file format.");
            }

            return ReadPNG(buffer);
        }

        private TextureData ReadPNG(byte[] buffer)
        {
            using var image = Image.Load<Rgba32>(buffer);

            int width = image.Width;
            int height = image.Height;

            if (width > MAX_WIDTH || height > MAX_HEIGHT)
            {
                throw new FileLoadException(
                    $"Invalid PNG/OZP Dimensions: Width={width}, Height={height}");
            }

            var data = new byte[width * height * 4];

            image.CopyPixelDataTo(data);

            return new TextureData
            {
                Width = width,
                Height = height,
                Components = 4,
                Data = data,
                IsCompressed = false,
                Format = TextureSurfaceFormat.Color
            };
        }
    }
}