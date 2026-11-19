using UnityEditor;

/// <summary>
/// 챕터 보스 그림(`Sprites/Boss/BossCh*.png`, BossArt)을 몹 시트와 같은 규칙으로 들인다 —
/// 한 장 스프라이트, 100 PPU(172칸이 몸집 1 에서 옛 보스의 몸집 2 와 같은 넓이), 발밑 가운데 피벗(SitOnFloor·StandOnFrame 이 아래끝을 잰다),
/// 점 필터·압축 없음(도트가 번지지 않게). 어드레서블 등록은 AddressableSetup.Required.
/// </summary>
public class BossArtImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Replace("\\", "/").Contains("/@Resources/Sprites/Boss/") == false)
            return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)UnityEngine.SpriteAlignment.BottomCenter;
        s.spriteMeshType = UnityEngine.SpriteMeshType.Tight;
        ti.SetTextureSettings(s);
        ti.filterMode = UnityEngine.FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
    }
}
