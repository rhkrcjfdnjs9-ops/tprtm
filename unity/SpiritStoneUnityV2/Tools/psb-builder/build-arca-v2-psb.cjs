const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
require('ag-psd/initialize-canvas');
const { writePsdBuffer, readPsd } = require('ag-psd');

const root = path.resolve(__dirname, '../../Assets/Characters/Arca/Rig');
const sourcePath = path.join(root, 'Source/Arca_RigMaster_V2_Transparent.png');
const partsRoot = path.join(root, 'PartsV2');
const outputPath = path.join(root, 'Arca_Rig_v2.psb');
const master = PNG.sync.read(fs.readFileSync(sourcePath));
fs.mkdirSync(partsRoot, { recursive: true });

function inside(x, y, p) {
  let yes = false;
  for (let i = 0, j = p.length / 2 - 1; i < p.length / 2; j = i++) {
    const xi=p[i*2], yi=p[i*2+1], xj=p[j*2], yj=p[j*2+1];
    if (((yi>y)!==(yj>y)) && x < (xj-xi)*(y-yi)/(yj-yi)+xi) yes=!yes;
  }
  return yes;
}

function cut(name, polygons) {
  const out = new PNG({width:master.width,height:master.height});
  for(let y=0;y<master.height;y++) for(let x=0;x<master.width;x++) {
    if(!polygons.some(p=>inside(x+.5,y+.5,p))) continue;
    const o=(y*master.width+x)*4;
    master.data.copy(out.data,o,o,o+4);
  }
  fs.writeFileSync(path.join(partsRoot,name+'.png'),PNG.sync.write(out));
  return {width:out.width,height:out.height,data:new Uint8ClampedArray(out.data)};
}

// Coordinates remain on the original 1254 x 1254 canvas. Joint masks overlap
// slightly so the rest pose has no seams and bone rotation has usable bleed.
const capeL=cut('Cape_Back_R',[[265,565,468,484,510,560,495,755,430,930,330,1080,270,1028,300,900,270,820]]);
const capeR=cut('Cape_Back_L',[[744,485,921,570,967,820,935,1080,842,930,762,760,744,560]]);
const hair=cut('Hair_Head',[[414,36,650,30,832,190,846,402,754,500,628,525,490,490,405,397]]);
const torso=cut('Torso',[[468,475,765,475,770,681,713,725,530,725,478,665]]);
const pelvis=cut('Pelvis_Skirt',[[451,650,795,650,808,840,705,864,620,835,532,865,442,820]]);
const upperR=cut('UpperArm_R',[[445,535,535,550,523,655,452,698,402,672]]);
const foreR=cut('Forearm_R',[[395,642,472,653,439,742,360,757,345,710]]);
const handR=cut('Hand_R',[[305,710,406,700,410,770,310,790,280,754]]);
const upperL=cut('UpperArm_L',[[718,545,805,535,852,672,798,700,728,650]]);
const foreL=cut('Forearm_L',[[790,650,865,638,909,708,891,759,812,742]]);
const handL=cut('Hand_L',[[858,700,955,710,980,755,944,792,850,770]]);
const thighR=cut('Thigh_R',[[483,794,620,795,613,1000,475,1005]]);
const bootR=cut('Boot_R',[[455,940,605,940,599,1218,460,1218]]);
const thighL=cut('Thigh_L',[[620,795,755,793,784,1005,635,1002]]);
const bootL=cut('Boot_L',[[642,940,795,940,792,1218,650,1218]]);

function composite(images){const d=new Uint8ClampedArray(master.width*master.height*4);for(const im of images)for(let o=0;o<d.length;o+=4){const a=im.data[o+3]/255,da=d[o+3]/255,oa=a+da*(1-a);if(!oa)continue;for(let c=0;c<3;c++)d[o+c]=Math.round((im.data[o+c]*a+d[o+c]*da*(1-a))/oa);d[o+3]=Math.round(oa*255);}return{width:master.width,height:master.height,data:d};}
const named={Cape_Back_R:capeL,Cape_Back_L:capeR,Hair_Head:hair,Torso:torso,
  Pelvis_Skirt:pelvis,UpperArm_R:upperR,Forearm_R:foreR,Hand_R:handR,
  UpperArm_L:upperL,Forearm_L:foreL,Hand_L:handL,Thigh_R:thighR,
  Boot_R:bootR,Thigh_L:thighL,Boot_L:bootL};
const baseLayers=Object.values(named);
const covered=composite(baseLayers);
function owner(x,y){
  if(y<515)return 'Hair_Head';
  if(x<445&&y>690&&y<805)return 'Hand_R';
  if(x>810&&y>690&&y<805)return 'Hand_L';
  if(x<485&&y>620&&y<805)return 'Forearm_R';
  if(x>770&&y>620&&y<805)return 'Forearm_L';
  if(x<535&&y>500&&y<710)return 'UpperArm_R';
  if(x>720&&y>500&&y<710)return 'UpperArm_L';
  if(x<475&&y>500)return 'Cape_Back_R';
  if(x>780&&y>500)return 'Cape_Back_L';
  if(y>930)return x<627?'Boot_R':'Boot_L';
  if(y>810)return x<627?'Thigh_R':'Thigh_L';
  if(y>650&&x>425&&x<825)return 'Pelvis_Skirt';
  return 'Torso';
}
let distributed=0;
for(let y=0;y<master.height;y++)for(let x=0;x<master.width;x++){
  const o=(y*master.width+x)*4;
  if(master.data[o+3]===0||covered.data[o+3]>0)continue;
  const target=named[owner(x,y)];
  for(let c=0;c<4;c++)target.data[o+c]=master.data[o+c];
  distributed++;
}
// Arm layers must be mutually exclusive. The previous visible-mask draft kept
// wrist overlap in both Forearm and Hand, which produced two hands under IK.
function subtract(base, overlay){
  for(let o=3;o<base.data.length;o+=4)if(overlay.data[o]>0){
    base.data[o-3]=0;base.data[o-2]=0;base.data[o-1]=0;base.data[o]=0;
  }
}
function subtractDilated(base, overlay, radius){
  const w=master.width,h=master.height;
  for(let y=0;y<h;y++)for(let x=0;x<w;x++){
    let covered=false;
    for(let dy=-radius;dy<=radius&&!covered;dy++)for(let dx=-radius;dx<=radius;dx++){
      const nx=x+dx,ny=y+dy;
      if(nx>=0&&ny>=0&&nx<w&&ny<h&&overlay.data[(ny*w+nx)*4+3]>0){covered=true;break;}
    }
    if(!covered)continue;
    const o=(y*w+x)*4;
    base.data[o]=0;base.data[o+1]=0;base.data[o+2]=0;base.data[o+3]=0;
  }
}
subtract(foreR,handR); subtract(upperR,foreR); subtract(upperR,handR);
subtract(foreL,handL); subtract(upperL,foreL); subtract(upperL,handL);
// The cape masks pass behind the arms, but cutting them directly from the
// master also copied the original arms and hands into the cape sprites. That
// left a second, stationary pair of hands visible whenever the rig moved.
// Keep only cape pixels wherever an articulated arm/hand owns the image.
subtract(capeL,upperR); subtract(capeL,foreR); subtract(capeL,handR);
subtract(capeR,upperL); subtract(capeR,foreL); subtract(capeR,handL);
// Clear the complete original hand silhouettes as well. Some antialiased edge
// pixels sit just outside the extracted Hand masks and otherwise appear as
// stationary extra fingertips when the articulated hands move.
subtractDilated(capeL,handR,5);
subtractDilated(capeR,handL,5);
for(const [name,image] of Object.entries(named)){
  const png=new PNG({width:master.width,height:master.height});
  png.data=Buffer.from(image.data);
  fs.writeFileSync(path.join(partsRoot,name+'.png'),PNG.sync.write(png));
}
const layers=baseLayers;
const doc={width:master.width,height:master.height,imageData:composite(layers),children:[{name:'ArcaV2',opened:true,children:[
  {name:'Front',opened:true,children:[{name:'Hair_Head',imageData:hair}]},
  {name:'Body',opened:true,children:[{name:'Torso',imageData:torso},{name:'Pelvis_Skirt',imageData:pelvis}]},
  {name:'Arm_R',opened:true,children:[{name:'UpperArm_R',imageData:upperR},{name:'Forearm_R',imageData:foreR},{name:'Hand_R',imageData:handR}]},
  {name:'Arm_L',opened:true,children:[{name:'UpperArm_L',imageData:upperL},{name:'Forearm_L',imageData:foreL},{name:'Hand_L',imageData:handL}]},
  {name:'Leg_R',opened:true,children:[{name:'Thigh_R',imageData:thighR},{name:'Boot_R',imageData:bootR}]},
  {name:'Leg_L',opened:true,children:[{name:'Thigh_L',imageData:thighL},{name:'Boot_L',imageData:bootL}]},
  {name:'Back',opened:true,children:[{name:'Cape_Back_R',imageData:capeL},{name:'Cape_Back_L',imageData:capeR}]},
  {name:'__REFERENCE_MASTER_V2',hidden:true,imageData:{width:master.width,height:master.height,data:new Uint8ClampedArray(master.data)}}
]}]};
fs.writeFileSync(outputPath,writePsdBuffer(doc,{psb:true,noBackground:true,compress:true}));
const header=fs.readFileSync(outputPath).subarray(0,6);
if(header.toString('ascii',0,4)!=='8BPS'||header.readUInt16BE(4)!==2)throw new Error('Invalid PSB');
const check=readPsd(fs.readFileSync(outputPath),{useImageData:true,skipThumbnail:true});
console.log(JSON.stringify({outputPath,size:fs.statSync(outputPath).size,width:check.width,height:check.height,partCount:layers.length,distributed}));
