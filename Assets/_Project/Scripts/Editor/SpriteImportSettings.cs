using UnityEditor;
using UnityEngine;

namespace RollAndRuin.EditorTools
{
    /// <summary>
    /// Setting import otomatis untuk sprite pixel art di Assets/_Project/Art/Sprites:
    /// Sprite, 16 PPU, Point filter, tanpa kompresi, mesh FullRect (supaya bisa di-tile).
    /// Sprite bernama *_9slice mendapat border 2px.
    /// </summary>
    public sealed class SpriteImportSettings : AssetPostprocessor
    {
        public const string SpritesRoot = "Assets/_Project/Art/Sprites/";
        public const float PixelsPerUnit = 16f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesRoot)) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        public static void Apply(TextureImporter importer, string path)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = path.Contains("_9slice") ? new Vector4(2, 2, 2, 2) : Vector4.zero;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("Roll & Ruin/Reimport Sprites")]
        public static void ReimportAll()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesRoot.TrimEnd('/') });
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var guid in guids)
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log($"[Roll & Ruin] {guids.Length} sprite di-import ulang.");
        }
    }
}
