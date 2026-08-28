const fs = require('fs');
const path = require('path');
const { PNG } = require('../psb-builder/node_modules/pngjs');
require('../psb-builder/node_modules/ag-psd/initialize-canvas');
const { writePsdBuffer, readPsd } = require('../psb-builder/node_modules/ag-psd');

const project = path.resolve(__dirname, '../..');
const sourcePath = path.join(project, 'Assets/Characters/Arca/Rig/Source/Arca_AnimationMaster_V3_Transparent.png');
const root = path.join(project, 'Assets/Characters/Arca/ProductionV4');
const parts = path.join(root, 'Parts');
const psbPath = path.join(root, 'Arca_ProductionV4.psb');
fs.mkdirSync(parts, { recursive: true });

const master = PNG.sync.read(fs.readFileSync(sourcePath));
if (master.width !== 1254 || master.height !== 1254) throw new Error('V3 canvas must remain 1254x1254');

function inside(x, y, polygon) {
  let result = false;
  for (let i=0,j=polygon.length/2-1;i<polygon.length/2;j=i++) {
    const xi=polygon[i*2], yi=polygon[i*2+1], xj=polygon[j*2], yj=polygon[j*2+1];
    if (((yi>y)!==(yj>y)) && x < (xj-xi)*(y-yi)/(yj-yi)+xi) result=!result;
  }
  return result;
}

function cut(polygons) {
  const out = new PNG({width:master.width,height:master.height});
  for(let y=0;y<master.height;y++) for(let x=0;x<master.width;x++) {
    if(!polygons.some(p=>inside(x+.5,y+.5,p))) continue;
    const o=(y*master.width+x)*4;
    master.data.copy(out.data,o,o,o+4);
  }
  return out;
}

// Ownership boundary for the first production part. It intentionally stops
// before both upper arms and the back cape; only the central torso artwork is
// allowed in this layer. Coordinates stay on the V3 master canvas.
const torso = cut([[
  536,470, 578,458, 627,466, 676,458, 718,470,
  716,520, 700,558, 698,620, 688,678,
  566,678, 556,620, 554,558, 538,520
]]);

const torsoPath=path.join(parts,'Torso.png');
fs.writeFileSync(torsoPath,PNG.sync.write(torso));
const torsoImage={width:torso.width,height:torso.height,data:new Uint8ClampedArray(torso.data)};
const reference={width:master.width,height:master.height,data:new Uint8ClampedArray(master.data)};
const doc={width:master.width,height:master.height,children:[
  {name:'Arca_ProductionV4',opened:true,children:[
    {name:'01_Body',opened:true,children:[{name:'Torso',imageData:torsoImage}]},
    {name:'__V3_REFERENCE_DO_NOT_EDIT',hidden:true,imageData:reference}
  ]}
]};
fs.writeFileSync(psbPath,writePsdBuffer(doc,{psb:true,noBackground:true,compress:true}));
const header=fs.readFileSync(psbPath).subarray(0,6);
if(header.toString('ascii',0,4)!=='8BPS'||header.readUInt16BE(4)!==2) throw new Error('Invalid PSB');
const check=readPsd(fs.readFileSync(psbPath),{useImageData:true,skipThumbnail:true});
console.log(JSON.stringify({psbPath,torsoPath,width:check.width,height:check.height,layers:['Torso']}));
