using System.IO;
using System.Text;
namespace BloodborneLauncher.Core;

/// <summary>Scan large engine binaries and logs without loading the entire file.</summary>
public static class StreamScan
{
    public static bool Contains(Stream stream,ReadOnlySpan<byte> needle)
    {
        if(needle.IsEmpty)return true;
        var buffer=new byte[checked(65536+needle.Length-1)];int overlap=0,count;
        while((count=stream.Read(buffer,overlap,buffer.Length-overlap))>0)
        {
            int length=overlap+count;
            if(buffer.AsSpan(0,length).IndexOf(needle)>=0)return true;
            overlap=Math.Min(length,needle.Length-1);
            buffer.AsSpan(length-overlap,overlap).CopyTo(buffer);
        }
        return false;
    }
    // The returned byte offset excludes a trailing incomplete line. The caller can resume there.
    public static long CompleteLines(Stream stream,Action<string> accept)
    {
        var buffer=new byte[65536];using var line=new MemoryStream();
        long position=stream.Position,complete=position;int count;
        while((count=stream.Read(buffer,0,buffer.Length))>0)
        {
            int start=0;
            for(int i=0;i<count;i++)
            {
                if(buffer[i]!='\n')continue;
                if(line.Length==0)accept(Encoding.UTF8.GetString(buffer,start,i-start));
                else
                {
                    line.Write(buffer,start,i-start);
                    accept(Encoding.UTF8.GetString(line.GetBuffer(),0,(int)line.Length));line.SetLength(0);
                }
                complete=position+i+1;start=i+1;
            }
            if(start<count)line.Write(buffer,start,count-start);
            position+=count;
        }
        return complete;
    }
}
