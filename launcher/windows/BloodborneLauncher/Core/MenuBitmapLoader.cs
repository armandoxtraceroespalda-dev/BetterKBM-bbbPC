using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace BloodborneLauncher.Core;

/// <summary>Decode local art without URI caches; retain only detached read-only native pixels.</summary>
public static class MenuBitmapLoader
{
    public static BitmapSource Load(string path,bool background=false,int backgroundWidth=1920)
    {
        using var input=File.OpenRead(Path.GetFullPath(path));
        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;
        if(background)image.DecodePixelWidth=Math.Clamp(backgroundWidth,960,1920);
        image.StreamSource=input;image.EndInit();image.Freeze();
        if(image.PixelWidth<1 || image.PixelHeight<1)throw new InvalidDataException(BloodborneLauncher.Core.UiText.Format("The image has no decoded pixels: {0}",path));
        return Detach(image);
    }

    /// <summary>One frozen native pixel surface; no decoder/atlas link or large managed staging array.</summary>
    internal static BitmapSource Detach(BitmapSource source)
    {
        BitmapSource pixels=source;
        if(source.Format!=PixelFormats.Pbgra32)
        { pixels=new FormatConvertedBitmap(source,PixelFormats.Pbgra32,null,0);pixels.Freeze(); }
        var result=new WriteableBitmap(pixels.PixelWidth,pixels.PixelHeight,96,96,PixelFormats.Pbgra32,null);
        result.Lock();
        try
        {
            var rect=new Int32Rect(0,0,result.PixelWidth,result.PixelHeight);
            pixels.CopyPixels(rect,result.BackBuffer,checked(result.BackBufferStride*result.PixelHeight),result.BackBufferStride);
            result.AddDirtyRect(rect);
        }
        finally { result.Unlock(); }
        // WPF freezes this into an ordinary read-only bitmap and retires the writable buffers.
        result.Freeze();return result;
    }

    /// <summary>Bounded read-only sample: a decoded PNG can still contain entirely invisible pixels.</summary>
    public static (byte Min,byte Max) SampleAlpha(BitmapSource source)
    {
        BitmapSource image=source;
        if(image.Format!=PixelFormats.Bgra32 && image.Format!=PixelFormats.Pbgra32)
        {
            image=new FormatConvertedBitmap(source,PixelFormats.Bgra32,null,0);image.Freeze();
        }
        int stride=checked(image.PixelWidth*4),rows=Math.Min(16,image.PixelHeight);
        var row=new byte[stride];byte min=255,max=0;
        for(int i=0;i<rows;i++)
        {
            int y=rows==1?0:(int)((long)i*(image.PixelHeight-1)/(rows-1));
            image.CopyPixels(new Int32Rect(0,y,image.PixelWidth,1),row,stride,0);
            for(int x=3;x<row.Length;x+=4){min=Math.Min(min,row[x]);max=Math.Max(max,row[x]);}
        }
        return(min,max);
    }
}
