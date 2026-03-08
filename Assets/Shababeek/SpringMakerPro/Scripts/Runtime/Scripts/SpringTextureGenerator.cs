using UnityEngine;

namespace Shababeek.Springs
{
    /// <summary>
    /// Generates procedural textures for spring line materials
    /// </summary>
    public static class SpringTextureGenerator
    {
        /// <summary>
        /// Creates a 64x64 energy-style radial texture
        /// </summary>
        /// <param name="size">Texture dimensions (width and height)</param>
        /// <returns>Generated texture</returns>
        public static Texture2D CreateEnergyTexture(int size = 64)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float v = (float)y / (size - 1);

                    float dist = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));

                    float intensity = Mathf.Clamp01(1f - dist * 2f);
                    intensity += Mathf.PerlinNoise(u * 8f, v * 8f) * 0.5f;

                    float colorShift = Mathf.Sin(dist * 20f) * 0.2f;

                    pixels[y * size + x] = new Color(
                        0.2f + colorShift,
                        0.8f + colorShift,
                        1f,
                        intensity);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
