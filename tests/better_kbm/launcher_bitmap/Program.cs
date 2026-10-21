using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BloodborneLauncher.Core;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        string directory=Path.Combine(Path.GetTempPath(),"bb-bitmap-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            string path=Path.Combine(directory,"native.png");
            var rgba=new byte[]{20,60,180,255,40,90,200,255,70,100,230,255,90,120,240,255};
            var bitmap=BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,rgba,8);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using(var output=File.Create(path))encoder.Save(output);
            var loaded=MenuBitmapLoader.Load(path);File.Delete(path);
            if(loaded is BitmapImage || loaded.Format!=PixelFormats.Pbgra32)throw new Exception("Cache must hold detached premultiplied pixels, not the PNG decoder.");
            if(MenuBitmapLoader.SampleAlpha(loaded)!=(255,255))throw new Exception("Opaque pixels must be recognized as visible.");
            var transparent=BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[16],8);
            if(MenuBitmapLoader.SampleAlpha(transparent)!=(0,0))throw new Exception("A decoded but invisible bitmap must be identifiable.");
            var mixed=(byte[])rgba.Clone();mixed[3]=0;
            if(MenuBitmapLoader.SampleAlpha(BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,mixed,8))!=(0,255))throw new Exception("Alpha diagnostics must preserve the source transparency.");
            var pixels=new byte[16];loaded.CopyPixels(pixels,8,0);
            if(!pixels.SequenceEqual(rgba) || !loaded.IsFrozen)throw new Exception("Decoded native pixels must survive closing/removing the source file.");
            var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawImage(loaded,new Rect(0,0,2,2));
            var rendered=new RenderTargetBitmap(2,2,96,96,PixelFormats.Pbgra32);rendered.Render(visual);
            var actual=new byte[16];rendered.CopyPixels(actual,8,0);
            if(!actual.SequenceEqual(rgba))throw new Exception("Loaded PNG must paint its original visible pixels.");
            string large=Path.Combine(directory,"wallpaper.png");
            var largeBitmap=BitmapSource.Create(1920,1024,96,96,PixelFormats.Bgra32,null,new byte[1920*1024*4],1920*4);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(largeBitmap));
            using(var output=File.Create(large))png.Save(output);
            _=MenuBitmapLoader.Load(large,true);
            long before=GC.GetAllocatedBytesForCurrentThread();var backdrop=MenuBitmapLoader.Load(large,true);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            if(allocated>1920L*1024*2)throw new Exception("Loading the backdrop must not allocate a full-resolution managed staging buffer.");
            File.Delete(large);
            if(backdrop.PixelWidth!=1920 || backdrop.PixelHeight!=1024 || !backdrop.IsFrozen)
                throw new Exception("Memory optimizations must preserve native backdrop dimensions and detached lifetime.");
            Console.WriteLine("PASS: detached premultiplied PNG decode, source file released, frozen image pixels and rendered output.");
        }
        finally{Directory.Delete(directory,true);}
    }
}
