using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReadBytesPic : MonoBehaviour
{
    public RawImage PreviewImg;
    public string BytesPicFileName;

    public int PicIdx = 0;
    
    private Texture2D _texture;
    // Start is called before the first frame update
    void Start()
    {
        if (string.IsNullOrEmpty(BytesPicFileName)) return;
        var asset = Resources.Load<TextAsset>(BytesPicFileName);
        // asset.bytes;
        _texture = new Texture2D(64, 64);
        for (var i = 0; i < 64; i++)
        {
            for (var j = 0; j < 64; j++)
            {
                var r = asset.bytes[PicIdx * 64 * 64 * 3 + i * 64 * 3 + j * 3];
                var g = asset.bytes[PicIdx * 64 * 64 * 3 + i * 64 * 3 + j * 3 + 1];
                var b = asset.bytes[PicIdx * 64 * 64 * 3 + i * 64 * 3 + j * 3 + 2];
                _texture.SetPixel(j, 64 - i, new Color(r / 255f, g / 255f, b / 255f));
            }
        }
        _texture.Apply();
        PreviewImg.texture = _texture;
    }
}
