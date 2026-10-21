using System;
namespace BloodborneLauncher.Core;

/// <summary>Join a native highlight once, so no two rendering surfaces meet at its center.</summary>
public static class MenuPixels
{
    public static byte[] PrepareHighlight(byte[] source,int width,int height,out int joinedWidth)
    {
        if(width<1 || height<1 || source.Length!=checked(width*height*4))throw new ArgumentException(BloodborneLauncher.Core.UiText.T("Invalid BGRA highlight bounds."));
        // The atlas crop includes transparent padding beyond the bright inner edge.
        // Join at the last maximum-energy column, not at that transparent border.
        int edge=width-1,best=0;
        for(int x=0;x<width;x++)
        {
            int alpha=0;for(int y=0;y<height;y++)alpha+=source[(y*width+x)*4+3];
            if(alpha>=best){best=alpha;edge=x;}
        }
        int usable=edge+1;var cropped=new byte[checked(usable*height*4)];
        for(int y=0;y<height;y++)Buffer.BlockCopy(source,y*width*4,cropped,y*usable*4,usable*4);
        joinedWidth=usable*2;return MirrorHighlight(cropped,usable,height);
    }
    public static byte[] MirrorHighlight(byte[] source,int width,int height)
    {
        if(width<1 || height<1 || source.Length!=checked(width*height*4))throw new ArgumentException(BloodborneLauncher.Core.UiText.T("Invalid BGRA highlight bounds."));
        var joined=new byte[checked(width*height*8)];int stride=width*8;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int from=(y*width+x)*4;
            Buffer.BlockCopy(source,from,joined,y*stride+x*4,4);
            Buffer.BlockCopy(source,from,joined,y*stride+(width*2-1-x)*4,4);
        }
        return joined;
    }
}
