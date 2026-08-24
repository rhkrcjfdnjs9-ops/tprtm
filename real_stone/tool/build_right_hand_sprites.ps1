$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class SpriteBuilder {
  static Bitmap Argb(Bitmap source) {
    var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(result)) {
      g.CompositingMode = CompositingMode.SourceCopy;
      g.DrawImageUnscaled(source, 0, 0);
    }
    return result;
  }

  static void ChromaKey(Bitmap image) {
    for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) {
      var c = image.GetPixel(x, y);
      if (c.G > 145 && c.G > c.R * 1.35 && c.G > c.B * 1.28)
        image.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
    }
  }

  static void FloodBackground(Bitmap image) {
    int w=image.Width,h=image.Height;
    var seen=new bool[w*h]; var q=new Queue<int>();
    Action<int,int> seed=(x,y)=>{int i=y*w+x;if(!seen[i]){seen[i]=true;q.Enqueue(i);}};
    for(int x=0;x<w;x++){seed(x,0);seed(x,h-1);} for(int y=0;y<h;y++){seed(0,y);seed(w-1,y);}
    int[] dx={1,-1,0,0},dy={0,0,1,-1};
    while(q.Count>0){int i=q.Dequeue(),x=i%w,y=i/w; var a=image.GetPixel(x,y); image.SetPixel(x,y,Color.FromArgb(0,0,0,0));
      for(int k=0;k<4;k++){int nx=x+dx[k],ny=y+dy[k];if(nx<0||ny<0||nx>=w||ny>=h)continue;int ni=ny*w+nx;if(seen[ni])continue;var b=image.GetPixel(nx,ny);int dr=a.R-b.R,dg=a.G-b.G,db=a.B-b.B;int spread=Math.Max(b.R,Math.Max(b.G,b.B))-Math.Min(b.R,Math.Min(b.G,b.B));if(dr*dr+dg*dg+db*db<625 && spread<75){seen[ni]=true;q.Enqueue(ni);}}
    }
  }

  static Rectangle Bounds(Bitmap b) {
    int l=b.Width,t=b.Height,r=-1,bot=-1;
    for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>18){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);}
    return r<l?Rectangle.Empty:Rectangle.FromLTRB(l,t,r+1,bot+1);
  }

  static double BodyAnchorX(Bitmap b, Rectangle box) {
    int left=b.Width,right=-1; int maxY=box.Top+(int)(box.Height*0.58);
    int safeLeft=box.Left+(int)(box.Width*0.18),safeRight=box.Right-(int)(box.Width*0.18);
    for(int y=box.Top;y<maxY;y++)for(int x=safeLeft;x<safeRight;x++){
      var c=b.GetPixel(x,y);if(c.A<=18)continue;int spread=Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B));bool hair=spread<55&&c.R>65;bool skin=c.R>165&&c.R>c.G+12&&c.G>65;if(hair||skin){left=Math.Min(left,x);right=Math.Max(right,x);}
    }
    return right>=left?(left+right)/2.0:(box.Left+box.Right)/2.0;
  }

  static void RemoveSmallEdgeFragments(Bitmap b) {
    int w=b.Width,h=b.Height; var seen=new bool[w*h]; int[] dx={1,-1,0,0,1,1,-1,-1},dy={0,0,1,-1,1,-1,1,-1};
    for(int sy=0;sy<h;sy++)for(int sx=0;sx<w;sx++){
      if(sx>0&&sx<w-1&&sy>0&&sy<h-1)continue; int start=sy*w+sx;if(seen[start]||b.GetPixel(sx,sy).A<=18)continue;
      var q=new Queue<int>();var pixels=new List<int>();seen[start]=true;q.Enqueue(start);
      while(q.Count>0){int i=q.Dequeue(),x=i%w,y=i/w;pixels.Add(i);for(int k=0;k<8;k++){int nx=x+dx[k],ny=y+dy[k];if(nx<0||ny<0||nx>=w||ny>=h)continue;int ni=ny*w+nx;if(!seen[ni]&&b.GetPixel(nx,ny).A>18){seen[ni]=true;q.Enqueue(ni);}}}
      if(pixels.Count<12000)foreach(int i in pixels)b.SetPixel(i%w,i/w,Color.FromArgb(0,0,0,0));
    }
  }

  public static void Sheet(string input,string output,string action,int cols,int rows,bool flood) {
    Directory.CreateDirectory(output); using(var raw=new Bitmap(input))using(var sheet=Argb(raw)){
      if(flood)FloodBackground(sheet);else ChromaKey(sheet);
      var cells=new List<Bitmap>();var boxes=new List<Rectangle>();var anchors=new List<double>();int mw=1,mh=1;
      for(int i=0;i<cols*rows;i++){int c=i%cols,r=i/cols,x0=(int)Math.Round(c*sheet.Width/(double)cols),x1=(int)Math.Round((c+1)*sheet.Width/(double)cols),y0=(int)Math.Round(r*sheet.Height/(double)rows),y1=(int)Math.Round((r+1)*sheet.Height/(double)rows);var cell=new Bitmap(x1-x0,y1-y0,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(cell)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(sheet,-x0,-y0);}RemoveSmallEdgeFragments(cell);var box=Bounds(cell);cells.Add(cell);boxes.Add(box);anchors.Add(BodyAnchorX(cell,box));mw=Math.Max(mw,box.Width);mh=Math.Max(mh,box.Height);}
      double scale=Math.Min(300.0/mw,232.0/mh);
      for(int i=0;i<cells.Count;i++){using(var frame=new Bitmap(320,256,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(frame)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;var b=boxes[i];int dw=Math.Max(1,(int)Math.Round(b.Width*scale)),dh=Math.Max(1,(int)Math.Round(b.Height*scale));int x=(int)Math.Round(160-(anchors[i]-b.Left)*scale);x=Math.Max(6,Math.Min(314-dw,x));int y=246-dh;g.DrawImage(cells[i],new Rectangle(x,y,dw,dh),b,GraphicsUnit.Pixel);frame.Save(Path.Combine(output,"grania_"+action+"_"+i+".png"),ImageFormat.Png);}cells[i].Dispose();}
    }
  }

  public static void Idle(string input,string output) {
    Directory.CreateDirectory(output);using(var raw=new Bitmap(input))using(var src=Argb(raw)){var b=Bounds(src);double scale=Math.Min(300.0/b.Width,232.0/b.Height);int dw=(int)Math.Round(b.Width*scale),dh=(int)Math.Round(b.Height*scale);double anchor=BodyAnchorX(src,b);int x=(int)Math.Round(160-(anchor-b.Left)*scale);x=Math.Max(6,Math.Min(314-dw,x));int[] bob={0,-1,-2,-1,0,1};for(int i=0;i<6;i++)using(var frame=new Bitmap(320,256,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(frame)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(src,new Rectangle(x,246-dh+bob[i],dw,dh),b,GraphicsUnit.Pixel);frame.Save(Path.Combine(output,"grania_idle_"+i+".png"),ImageFormat.Png);}}
  }
}
'@
Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing
$root=Split-Path -Parent $PSScriptRoot
$ref=Join-Path $root 'assets\references\right_hand_redesign'
$sheets=Join-Path $ref 'sheets'
$out4=Join-Path $root 'assets\frames\stage_5_right_hand'
$out5=Join-Path $root 'assets\frames\stage_6_right_hand'
[SpriteBuilder]::Idle((Join-Path $ref 'grania_stage4_master_transparent.png'),$out4)
[SpriteBuilder]::Idle((Join-Path $ref 'grania_stage5_master_transparent.png'),$out5)
foreach($spec in @(
  @('stage4_walk_raw.png',$out4,'walk',4,2,$false),@('stage4_attack_raw.png',$out4,'attack',4,2,$false),@('stage4_hit_raw.png',$out4,'hit',5,1,$false),@('stage4_death_raw.png',$out4,'death',3,2,$true),
  @('stage5_walk_raw.png',$out5,'walk',4,2,$false),@('stage5_attack_raw.png',$out5,'attack',4,2,$false),@('stage5_hit_raw.png',$out5,'hit',5,1,$false),@('stage5_death_raw.png',$out5,'death',3,2,$false)
)) {[SpriteBuilder]::Sheet((Join-Path $sheets $spec[0]),$spec[1],$spec[2],[int]$spec[3],[int]$spec[4],[bool]$spec[5])}
Write-Output "Built right-hand sprite sets: $out4 and $out5"
