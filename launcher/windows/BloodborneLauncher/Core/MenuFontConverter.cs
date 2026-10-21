using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
namespace BloodborneLauncher.Core;

/// <summary>Converts DefineFont3 quadratic outlines to a local Unicode TrueType font.</summary>
public static class MenuFontConverter
{
    private sealed class Bits
    {
        private readonly byte[] data;private int bit;
        public Bits(byte[] data,int offset=0){this.data=data;bit=checked(offset*8);}
        public int U(int count){if(count<0 || count>31 || (long)bit+count>(long)data.Length*8)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Truncated font outline."));int v=0;for(int i=0;i<count;i++,bit++)v=(v<<1)|((data[bit/8]>>(7-bit%8))&1);return v;}
        public int S(int count){if(count==0)return 0;int v=U(count);return (v&(1<<(count-1)))!=0?v-(1<<count):v;}
    }
    private sealed class BE : IDisposable
    {
        public readonly MemoryStream Stream=new();
        public void U16(int v){Stream.WriteByte((byte)(v>>8));Stream.WriteByte((byte)v);}
        public void U32(uint v){U16((int)(v>>16));U16((int)v);}
        public void Bytes(byte[] v)=>Stream.Write(v);
        public void Zero(int count){for(int i=0;i<count;i++)Stream.WriteByte(0);}
        public byte[] Data=>Stream.ToArray();
        public void Dispose()=>Stream.Dispose();
    }
    private readonly record struct Point(short X,short Y,bool On);
    private sealed record Glyph(byte[] Data,short Left,short Bottom,short Right,short Top,int Points,int Contours);
    private static ushort U16(byte[] b,int at)=>BitConverter.ToUInt16(b,at);
    private static int I16(byte[] b,int at)=>BitConverter.ToInt16(b,at);
    private static int U32(byte[] b,int at)=>checked((int)BitConverter.ToUInt32(b,at));
    private static Glyph ReadGlyph(byte[] font,int at)
    {
        var bits=new Bits(font,at);int fill=bits.U(4),line=bits.U(4),x=0,y=0;var contours=new List<List<Point>>();List<Point>? current=null;
        Point Position(bool on)=>new(checked((short)Math.Round(x/20.0)),checked((short)Math.Round(-y/20.0)),on);
        void Close(){if(current is {Count:>0}){if(current.Count>1 && current[0]==current[^1])current.RemoveAt(current.Count-1);contours.Add(current);}current=null;}
        for(int record=0;record<100000;record++)
        {
            if(bits.U(1)==0)
            {
                int style=bits.U(5);if(style==0){Close();break;}
                if((style&1)!=0){Close();int n=bits.U(5);x=bits.S(n);y=bits.S(n);}
                if((style&2)!=0)bits.U(fill);if((style&4)!=0)bits.U(fill);if((style&8)!=0)bits.U(line);
                if((style&16)!=0)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Unexpected font style table."));
            }
            else
            {
                current??=new List<Point>{Position(true)};int straight=bits.U(1),n=bits.U(4)+2;
                if(straight!=0){if(bits.U(1)!=0){x+=bits.S(n);y+=bits.S(n);}else if(bits.U(1)!=0)y+=bits.S(n);else x+=bits.S(n);current.Add(Position(true));}
                else {x+=bits.S(n);y+=bits.S(n);current.Add(Position(false));x+=bits.S(n);y+=bits.S(n);current.Add(Position(true));}
            }
            if(record==99999)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Font outline exceeds the record limit."));
        }
        var points=contours.SelectMany(c=>c).ToArray();if(points.Length==0)return new(Array.Empty<byte>(),0,0,0,0,0,0);
        if(points.Length>65535 || contours.Count>32767)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Font glyph is too complex."));
        short left=points.Min(p=>p.X),bottom=points.Min(p=>p.Y),right=points.Max(p=>p.X),top=points.Max(p=>p.Y);
        using var w=new BE();w.U16(contours.Count);w.U16(left);w.U16(bottom);w.U16(right);w.U16(top);int end=0;
        foreach(var c in contours){end+=c.Count;w.U16(end-1);}w.U16(0);
        foreach(var p in points)w.Stream.WriteByte((byte)(p.On?1:0));int previous=0;
        foreach(var p in points){w.U16(checked((short)(p.X-previous)));previous=p.X;}previous=0;
        foreach(var p in points){w.U16(checked((short)(p.Y-previous)));previous=p.Y;}
        return new(w.Data,left,bottom,right,top,points.Length,contours.Count);
    }
    public static void Convert(string source,string output,CancellationToken cancel=default)
    {
        var swf=File.ReadAllBytes(source);if(swf.Length<16)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Truncated font bundle."));
        if(!swf.AsSpan(0,3).SequenceEqual("GFX"u8) && !swf.AsSpan(0,3).SequenceEqual("FWS"u8))throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Unsupported compressed font bundle."));
        int at=8+(5+4*(swf[8]>>3)+7)/8+4;byte[]? font=null;
        while(at<=swf.Length-2)
        {
            int tag=U16(swf,at);at+=2;int size=tag&63;if(size==63){size=U32(swf,at);at+=4;}
            if(size<0 || at>swf.Length-size)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Invalid font tag bounds."));
            if((tag>>6)==75){font=swf.AsSpan(at,size).ToArray();break;}at+=size;
        }
        if(font==null || font.Length<7)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("No DefineFont3 outlines found."));
        int flags=font[2],nameLength=font[4],count=U16(font,5+nameLength),start=7+nameLength;
        if(count==0 || count>=65535)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Invalid font glyph count."));
        bool wide=(flags&8)!=0;int stride=wide?4:2;int Offset(int p)=>wide?U32(font,p):U16(font,p);
        int codesAt=checked(start+Offset(start+count*stride)),codeSize=(flags&4)!=0?2:1;
        if(codesAt<0 || codesAt>font.Length-count*codeSize)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Invalid font character table."));
        var glyphs=new List<Glyph>{new(Array.Empty<byte>(),0,0,0,0,0,0)};var advances=new List<ushort>{512};var cmap=new SortedDictionary<ushort,ushort>();
        int asc=820,desc=-204,layout=codesAt+count*codeSize;
        if((flags&128)!=0){asc=(int)Math.Round(I16(font,layout)/20.0);desc=-Math.Abs((int)Math.Round(I16(font,layout+2)/20.0));}
        for(int i=0;i<count;i++)
        {
            cancel.ThrowIfCancellationRequested();int offset=checked(start+Offset(start+i*stride));
            if(offset<start || offset>=codesAt)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Invalid glyph offset."));
            glyphs.Add(ReadGlyph(font,offset));int advance=(flags&128)!=0?I16(font,layout+6+i*2):10240;
            advances.Add((ushort)Math.Clamp((int)Math.Round(advance/20.0),0,65535));
            ushort code=(ushort)(codeSize==2?U16(font,codesAt+i*2):font[codesAt+i]);if(code!=65535)cmap[code]=(ushort)(i+1);
        }
        var tables=new SortedDictionary<string,byte[]>();using(var glyf=new BE())using(var loca=new BE())using(var hmtx=new BE())
        {
            foreach(var (g,i) in glyphs.Select((g,i)=>(g,i))){loca.U32((uint)glyf.Stream.Length);glyf.Bytes(g.Data);while(glyf.Stream.Length%4!=0)glyf.Zero(1);hmtx.U16(advances[i]);hmtx.U16(g.Left);}
            loca.U32((uint)glyf.Stream.Length);tables["glyf"]=glyf.Data;tables["loca"]=loca.Data;tables["hmtx"]=hmtx.Data;
        }
        short xmin=glyphs.Min(g=>g.Left),ymin=glyphs.Min(g=>g.Bottom),xmax=glyphs.Max(g=>g.Right),ymax=glyphs.Max(g=>g.Top);
        using(var w=new BE()){w.U32(0x10000);w.U32(0x10000);w.U32(0);w.U32(0x5F0F3CF5);w.U16(11);w.U16(1024);w.U32(0);w.U32(3660681600);w.U32(0);w.U32(3660681600);w.U16(xmin);w.U16(ymin);w.U16(xmax);w.U16(ymax);w.U16(0);w.U16(8);w.U16(2);w.U16(1);w.U16(0);tables["head"]=w.Data;}
        using(var w=new BE()){w.U32(0x10000);w.U16(asc);w.U16(desc);w.U16(0);w.U16(advances.Max());w.U16(xmin);w.U16(glyphs.Select((g,i)=>(int)advances[i]-g.Right).Min());w.U16(xmax);w.U16(1);w.U16(0);w.U16(0);w.Zero(8);w.U16(0);w.U16(glyphs.Count);tables["hhea"]=w.Data;}
        using(var w=new BE()){w.U32(0x10000);foreach(int n in new[]{glyphs.Count,glyphs.Max(g=>g.Points),glyphs.Max(g=>g.Contours),0,0,2,0,0,0,0,0,0,0,0})w.U16(n);tables["maxp"]=w.Data;}
        using(var w=new BE())
        {
            int segments=cmap.Count+1,power=1,selector=0;while(power*2<=segments){power*=2;selector++;}
            if(16+8*segments>65535)throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Menu font exceeds the BMP cmap segment limit."));
            w.U16(0);w.U16(2);w.U16(0);w.U16(3);w.U32(20);w.U16(3);w.U16(1);w.U32(20);
            w.U16(4);w.U16(16+8*segments);w.U16(0);w.U16(segments*2);w.U16(power*2);w.U16(selector);w.U16(segments*2-power*2);
            foreach(var code in cmap.Keys)w.U16(code);w.U16(65535);w.U16(0);foreach(var code in cmap.Keys)w.U16(code);w.U16(65535);
            foreach(var p in cmap)w.U16(p.Value-p.Key);w.U16(1);w.Zero(segments*2);tables["cmap"]=w.Data;
        }
        var names=new Dictionary<int,string>{{1,"Bloodborne Menu Local"},{2,"Regular"},{3,"LocalBloodborneMenu"},{4,"Bloodborne Menu Local"},{5,"Version 1.0"},{6,"BloodborneMenuLocal"}};
        using(var w=new BE())using(var strings=new BE()){w.U16(0);w.U16(names.Count);w.U16(6+12*names.Count);foreach(var n in names){var bytes=Encoding.BigEndianUnicode.GetBytes(n.Value);w.U16(3);w.U16(1);w.U16(0x409);w.U16(n.Key);w.U16(bytes.Length);w.U16((int)strings.Stream.Length);strings.Bytes(bytes);}w.Bytes(strings.Data);tables["name"]=w.Data;}
        using(var w=new BE()){w.U32(0x30000);w.U32(0);w.U16(-100);w.U16(50);w.Zero(20);tables["post"]=w.Data;}
        using(var w=new BE())
        {
            w.U16(0);w.U16((int)advances.Average(v=>(int)v));w.U16(400);w.U16(5);w.U16(0);
            foreach(int n in new[]{650,600,0,75,650,600,0,350,50,150,0})w.U16(n);
            w.Zero(10);w.U32(0x20F);w.U32(0);w.U32(0);w.U32(0);w.Bytes(Encoding.ASCII.GetBytes("LOCL"));w.U16(0x40);w.U16(cmap.Keys.Min());w.U16(cmap.Keys.Max());w.U16(asc);w.U16(desc);w.U16(0);w.U16(Math.Max(asc,ymax));w.U16(Math.Max(-desc,-ymin));tables["OS/2"]=w.Data;
        }
        WriteFont(tables,output);
    }
    private static uint Checksum(byte[] b){uint sum=0;for(int i=0;i<b.Length;i+=4){uint word=0;for(int j=0;j<4;j++)word=(word<<8)|(i+j<b.Length?b[i+j]:0u);sum=unchecked(sum+word);}return sum;}
    private static void WriteFont(SortedDictionary<string,byte[]> tables,string path)
    {
        int power=1,selector=0;while(power*2<=tables.Count){power*=2;selector++;}using var w=new BE();w.U32(0x10000);w.U16(tables.Count);w.U16(power*16);w.U16(selector);w.U16(tables.Count*16-power*16);
        int offset=12+16*tables.Count,headOffset=0;foreach(var table in tables){w.Bytes(Encoding.ASCII.GetBytes(table.Key));w.U32(Checksum(table.Value));w.U32((uint)offset);w.U32((uint)table.Value.Length);if(table.Key=="head")headOffset=offset;offset+=(table.Value.Length+3)/4*4;}
        foreach(var table in tables){w.Bytes(table.Value);while(w.Stream.Length%4!=0)w.Zero(1);}var result=w.Data;uint adjustment=unchecked(0xB1B0AFBA-Checksum(result));
        for(int i=0;i<4;i++)result[headOffset+8+i]=(byte)(adjustment>>(24-8*i));File.WriteAllBytes(path,result);
    }
}
