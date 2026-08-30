// LibreSprite script: deterministic 64x64 Arca base sprite.
const document = app.open("C:/Users/rhkrc/orca/projects/tprtm/unity/SpiritStoneUnityV2/Tools/PixelArt/blank64.png");
const img = document.sprite.layer(0).cel(0).image;
const pc = app.pixelColor;
const C = {
  o: pc.rgba(27,16,43,255), d: pc.rgba(41,19,72,255), s: pc.rgba(69,32,111,255),
  p: pc.rgba(112,56,168,255), v: pc.rgba(157,86,216,255), l: pc.rgba(212,155,255,255),
  w: pc.rgba(255,245,255,255), k: pc.rgba(23,19,30,255), c: pc.rgba(40,33,48,255),
  g: pc.rgba(215,154,56,255), y: pc.rgba(255,212,122,255), skin: pc.rgba(244,183,159,255),
  hi: pc.rgba(255,216,198,255), blush: pc.rgba(233,130,139,255), eye: pc.rgba(185,105,240,255),
  ed: pc.rgba(86,39,127,255), boot: pc.rgba(33,25,43,255)
};
function px(x,y,c){ if(x>=0&&x<64&&y>=0&&y<64) img.putPixel(x,y,c); }
function rect(x1,y1,x2,y2,c){ for(let y=y1;y<=y2;y++)for(let x=x1;x<=x2;x++)px(x,y,c); }
function ellipse(cx,cy,rx,ry,c){ for(let y=cy-ry;y<=cy+ry;y++)for(let x=cx-rx;x<=cx+rx;x++){let dx=(x-cx)/rx,dy=(y-cy)/ry;if(dx*dx+dy*dy<=1)px(x,y,c);} }
function poly(points,c){
  let minY=63,maxY=0; for(const p of points){minY=Math.min(minY,p[1]);maxY=Math.max(maxY,p[1]);}
  for(let y=minY;y<=maxY;y++){
    let nodes=[],j=points.length-1;
    for(let i=0;i<points.length;i++){let a=points[i],b=points[j];if((a[1]<y&&b[1]>=y)||(b[1]<y&&a[1]>=y))nodes.push(Math.round(a[0]+(y-a[1])/(b[1]-a[1])*(b[0]-a[0])));j=i;}
    nodes.sort((a,b)=>a-b);for(let i=0;i+1<nodes.length;i+=2)for(let x=nodes[i];x<=nodes[i+1];x++)px(x,y,c);
  }
}

// Lightning ornament.
poly([[33,4],[38,4],[35,9],[39,9],[32,14],[34,11],[30,11]],C.o);
poly([[34,5],[36,5],[33,10],[36,10],[33,13],[34,10],[32,10]],C.l);
// Hair silhouette and side locks.
ellipse(32,22,19,17,C.o); ellipse(32,22,17,15,C.p);
poly([[14,22],[17,31],[13,37],[21,34],[24,26]],C.o); poly([[15,23],[18,31],[15,34],[20,32],[22,25]],C.s);
poly([[49,21],[50,31],[54,35],[47,35],[43,26]],C.o); poly([[48,22],[48,30],[51,33],[47,32],[44,25]],C.v);
// Face.
rect(16,21,19,27,C.o);rect(17,22,19,26,C.skin);rect(45,21,48,27,C.o);rect(45,22,47,26,C.skin);
ellipse(32,24,13,11,C.o);ellipse(32,24,12,10,C.skin);rect(22,19,42,27,C.hi);
// Chunky bangs.
ellipse(32,17,16,10,C.p);
poly([[16,17],[21,10],[30,8],[27,20],[23,25],[23,16]],C.s);
poly([[26,9],[35,8],[34,22],[30,26],[29,16]],C.p);
poly([[34,8],[44,13],[43,23],[38,27],[39,15]],C.v);
rect(22,12,25,14,C.v);rect(26,10,31,12,C.v);rect(37,11,41,13,C.l);rect(40,14,43,16,C.l);
// Expression.
rect(23,20,28,21,C.o);rect(36,20,41,21,C.o);rect(23,22,28,27,C.o);rect(36,22,41,27,C.o);
rect(24,22,27,26,C.w);rect(37,22,40,26,C.w);rect(25,23,27,26,C.eye);rect(37,23,39,26,C.eye);
rect(26,24,27,26,C.ed);rect(37,24,38,26,C.ed);px(25,22,C.w);px(39,22,C.w);
rect(20,28,23,29,C.blush);rect(41,28,44,29,C.blush);rect(30,29,35,30,C.o);rect(31,29,34,29,C.blush);
// Hair clip.
poly([[18,14],[22,11],[23,14],[26,15],[22,18],[20,17]],C.o);poly([[20,14],[22,13],[22,15],[24,15],[21,17],[21,15]],C.y);
// Cape.
poly([[21,33],[16,48],[22,46],[25,53],[29,43]],C.o);poly([[22,34],[18,46],[22,44],[25,49],[27,40]],C.s);
poly([[43,33],[49,48],[44,46],[40,52],[37,42]],C.o);poly([[42,34],[47,46],[43,44],[40,49],[38,40]],C.p);
rect(18,45,20,46,C.v);rect(44,44,46,45,C.l);
// Torso.
poly([[25,32],[39,32],[43,43],[38,47],[26,47],[21,43]],C.o);poly([[26,33],[38,33],[40,42],[36,45],[28,45],[24,42]],C.c);
rect(27,34,37,36,C.k);poly([[30,33],[32,31],[34,33],[32,36]],C.g);px(32,33,C.l);rect(26,39,38,40,C.g);rect(28,39,36,39,C.y);
// Separated arms and hands.
poly([[23,34],[18,36],[14,42],[17,45],[22,40],[27,38]],C.o);poly([[22,35],[19,37],[16,42],[17,43],[21,39],[25,37]],C.skin);
rect(15,40,18,44,C.c);px(15,44,C.hi);px(17,44,C.hi);rect(19,37,22,38,C.g);
poly([[40,34],[45,35],[51,39],[50,43],[44,40],[37,38]],C.o);poly([[41,35],[44,36],[49,39],[49,41],[45,39],[39,37]],C.hi);
rect(47,38,51,42,C.c);px(51,40,C.skin);px(51,42,C.skin);rect(43,36,46,37,C.y);
// Skirt.
poly([[24,43],[40,43],[45,50],[38,52],[32,50],[26,52],[19,50]],C.o);poly([[24,44],[40,44],[42,49],[37,50],[32,48],[27,50],[22,49]],C.k);
poly([[22,49],[27,50],[32,48],[37,50],[42,49],[40,52],[35,51],[32,53],[28,51],[23,52]],C.v);px(24,49,C.l);px(31,49,C.l);px(39,49,C.l);
// Legs and boots, fixed baseline y=61.
poly([[25,50],[31,50],[30,57],[28,57],[28,61],[21,61],[22,57],[24,56]],C.o);poly([[26,51],[29,51],[28,56],[25,56]],C.hi);
poly([[22,56],[29,56],[28,60],[22,60]],C.boot);rect(23,57,28,58,C.p);px(26,57,C.y);
poly([[34,50],[40,50],[41,56],[43,57],[43,61],[35,61],[35,57]],C.o);poly([[35,51],[39,51],[39,56],[36,56]],C.skin);
poly([[36,56],[42,56],[42,60],[35,60]],C.boot);rect(37,57,41,58,C.p);px(38,57,C.y);

document.sprite.commit();
document.sprite.saveAs("C:/Users/rhkrc/orca/projects/tprtm/unity/SpiritStoneUnityV2/Assets/Characters/Arca/Pixel64/Arca_SD64_Production_v1.png", false);
