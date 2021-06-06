using System.IO;

using UnityEngine;
using UnityEditor.AssetImporters;

namespace WebP.Unity
{
    using WebP_SimpleJSON;

    [ScriptedImporter(1, "webp")]
    public class WebPImporter : ScriptedImporter
    {
        [Space]
        [SerializeField] private float pixelsPerUnit = 100f;
        [SerializeField] private SpriteMeshType spriteMeshType = SpriteMeshType.FullRect;
        [SerializeField] private Vector2 pivot = Vector2.one * .5f;

        [Space]
        [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        [SerializeField] private FilterMode filterMode = FilterMode.Point;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var bytes = File.ReadAllBytes(ctx.assetPath);
            var atlas = WebPHelper.CreateTexture2D(bytes, false, true, out var errorReport);

            if (errorReport != Error.Success)
                throw new System.Exception("Cannot import webp file !");

            atlas.filterMode = this.filterMode;
            atlas.wrapMode = this.wrapMode;

            ctx.AddObjectToAsset("atlas", atlas);
            ctx.SetMainObject(atlas);

            GenegrateSheet(ctx, atlas, ctx.assetPath.Replace(".webp", ".json"));
        }

        private void GenegrateSheet(AssetImportContext ctx, Texture2D atlas, string filePath)
        {
            if (File.Exists(filePath))
            {
                var allText = File.ReadAllText(filePath);

                foreach (var pair in JSON.Parse(allText))
                {
                    var key = pair.Key.Substring(2);
                    var value = pair.Value;
                    var frame = value["frame"];
                    var frameX = frame["x"].AsInt;
                    var frameY = frame["y"].AsInt;
                    var frameW = frame["width"].AsInt;
                    var frameH = frame["height"].AsInt;
                    var rect = GetSpriteRect(frameX, frameY, frameW, frameH, atlas.height);

                    var sprite = Sprite.Create(atlas, rect, this.pivot, this.pixelsPerUnit, 0, this.spriteMeshType, Vector4.zero, false);
                    sprite.name = key;

                    ctx.AddObjectToAsset(key, sprite);
                }
            }
        }

        private static Rect GetSpriteRect(int x, int y, int w, int h, int atlasHeight)
        {
            return new Rect(
                x,
                atlasHeight - h - y,
                w,
                h);
        }
    }
}