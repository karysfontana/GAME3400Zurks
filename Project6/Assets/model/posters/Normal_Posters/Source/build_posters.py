from pathlib import Path
import shutil,struct,json,hashlib
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT.parent/'upload'
ITEMS=[('Poster_01_Riot','f074490bf57846e12d4e05d50df62ae3.jpg'),('Poster_02_Seventeen','eb82def9763c67299979db93e9dd3d68.jpg'),('Poster_03_PrettyLittleLiars','be66db180de87a1a4158dfc61b1b5950.jpg')]
# Original image bytes copied unchanged. Artwork fits by mesh dimensions, not image editing.
def geometry(iw,ih):
    fit=min(.5/iw,.65/ih);aw=iw*fit;ah=ih*fit
    parts=[]
    def quad(name,corners,normal,mat):
        parts.append(dict(name=name,v=corners,n=[normal]*4,uv=[(0,0),(1,0),(1,1),(0,1)],f=[(0,1,2),(0,2,3)],mat=mat))
    x=.25;y=.325;a=aw/2;b=ah/2;z=.0004
    quad('Printed_Artwork',[(-a,-b,z),(a,-b,z),(a,b,z),(-a,b,z)],(0,0,1),0)
    if b<y-1e-7:
        quad('Paper_Top',[(-x,b,z),(x,b,z),(x,y,z),(-x,y,z)],(0,0,1),1)
        quad('Paper_Bottom',[(-x,-y,z),(x,-y,z),(x,-b,z),(-x,-b,z)],(0,0,1),1)
    if a<x-1e-7:
        quad('Paper_Left',[(-x,-b,z),(-a,-b,z),(-a,b,z),(-x,b,z)],(0,0,1),1)
        quad('Paper_Right',[(a,-b,z),(x,-b,z),(x,b,z),(a,b,z)],(0,0,1),1)
    quad('Paper_Back',[(x,-y,-z),(-x,-y,-z),(-x,y,-z),(x,y,-z)],(0,0,-1),1)
    quad('Paper_Right_Edge',[(x,-y,z),(x,-y,-z),(x,y,-z),(x,y,z)],(1,0,0),1)
    quad('Paper_Left_Edge',[(-x,-y,-z),(-x,-y,z),(-x,y,z),(-x,y,-z)],(-1,0,0),1)
    quad('Paper_Top_Edge',[(-x,y,z),(x,y,z),(x,y,-z),(-x,y,-z)],(0,1,0),1)
    quad('Paper_Bottom_Edge',[(-x,-y,-z),(x,-y,-z),(x,-y,z),(-x,-y,z)],(0,-1,0),1)
    return parts,(aw,ah)

def glb(name,parts,jpg):
    blob=bytearray();views=[];acc=[]
    def data(b,target=None):
        blob.extend(b'\0'*((-len(blob))%4));view={'buffer':0,'byteOffset':len(blob),'byteLength':len(b)}
        if target:view['target']=target
        blob.extend(b);views.append(view);return len(views)-1
    def arr(a,typ,comp,target):
        flat=[i for row in a for i in row] if typ!='SCALAR' else a
        fmt='f' if comp==5126 else 'H'
        idx=data(struct.pack('<'+fmt*len(flat),*flat),target)
        item={'bufferView':idx,'componentType':comp,'count':len(a),'type':typ}
        if typ=='VEC3':item.update(min=[min(v[i] for v in a) for i in range(3)],max=[max(v[i] for v in a) for i in range(3)])
        acc.append(item);return len(acc)-1
    meshes=[];nodes=[]
    for p in parts:
        attrs={'POSITION':arr(p['v'],'VEC3',5126,34962),'NORMAL':arr(p['n'],'VEC3',5126,34962),'TEXCOORD_0':arr([(u,1-v) for u,v in p['uv']],'VEC2',5126,34962)}
        fi=arr([x for row in p['f'] for x in row],'SCALAR',5123,34963)
        meshes.append({'name':p['name'],'primitives':[{'attributes':attrs,'indices':fi,'material':p['mat']}]});nodes.append({'name':p['name'],'mesh':len(meshes)-1})
    image=data(jpg)
    d={'asset':{'version':'2.0','generator':'Normal poster builder'},'scene':0,'scenes':[{'nodes':list(range(len(nodes)))}],'nodes':nodes,'meshes':meshes,'materials':[{'name':name+'_Print','pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':0,'roughnessFactor':.9}},{'name':'Paper_White','pbrMetallicRoughness':{'baseColorFactor':[.97,.96,.94,1],'metallicFactor':0,'roughnessFactor':.95}}],'textures':[{'source':0,'sampler':0}],'samplers':[{'magFilter':9729,'minFilter':9987,'wrapS':33071,'wrapT':33071}],'images':[{'bufferView':image,'mimeType':'image/jpeg'}],'buffers':[{'byteLength':len(blob)}],'bufferViews':views,'accessors':acc}
    js=json.dumps(d,separators=(',',':')).encode();js+=b' '*((-len(js))%4);blob+=b'\0'*((-len(blob))%4)
    return struct.pack('<III',0x46546c67,2,28+len(js)+len(blob))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(blob),0x004e4942)+blob

stats={}
for name,source in ITEMS:
    src=SRC/source;dst=ROOT/'Textures'/(name+'_BaseColor.jpg');shutil.copyfile(src,dst)
    assert src.read_bytes()==dst.read_bytes()
    iw,ih=Image.open(src).size;parts,art=geometry(iw,ih)
    lines=['# meters; Y up; front +Z; pivot at paper center','mtllib '+name+'.mtl'];offset=1
    for p in parts:
        lines+=['o '+p['name'],'usemtl '+(name+'_Print' if p['mat']==0 else 'Paper_White')]
        lines+=['v %.7f %.7f %.7f'%tuple(v) for v in p['v']]
        lines+=['vt %.7f %.7f'%tuple(v) for v in p['uv']]
        lines+=['vn %.7f %.7f %.7f'%tuple(v) for v in p['n']]
        lines+=['f '+' '.join(f'{i+offset}/{i+offset}/{i+offset}' for i in face) for face in p['f']];offset+=4
    (ROOT/(name+'.obj')).write_text('\n'.join(lines))
    (ROOT/(name+'.mtl')).write_text('newmtl '+name+'_Print\nKd 1 1 1\nKs 0 0 0\nd 1\nillum 2\nmap_Kd Textures/'+name+'_BaseColor.jpg\n\nnewmtl Paper_White\nKd 0.97 0.96 0.94\nKs 0 0 0\nd 1\nillum 2\n')
    (ROOT/(name+'.glb')).write_bytes(glb(name,parts,src.read_bytes()))
    stats[name]={'paper_m':[.5,.65,.0008],'artwork_m':art,'original_pixels':[iw,ih],'triangles':sum(len(p['f']) for p in parts),'source_sha256':hashlib.sha256(src.read_bytes()).hexdigest()}
(ROOT/'Model_Info.json').write_text(json.dumps(stats,indent=2))
# Local interactive viewer of the original textures on the model's exact flat outline.
# No raster source changes: aspect-fit via CSS, same geometry and UV layout as the exports.
cards=''.join('<div class="card"><div class="paper"><img src="Textures/'+name+'_BaseColor.jpg"></div><p>'+label+'</p></div>' for (name,_),label in zip(ITEMS,['01 · RIOT','02 · SEVENTEEN','03 · PRETTY LITTLE LIARS']))
(ROOT/'Preview.html').write_text('''<!doctype html><html><head><meta charset="utf-8"><title>Normal Posters</title><style>body{margin:0;background:#e9e7e3;font:16px Arial;color:#32312e;padding:40px}h1{font-size:24px;font-weight:500}main{display:flex;gap:30px;flex-wrap:wrap}.card{width:300px}.paper{width:300px;height:390px;background:rgb(247,245,240);display:flex;align-items:center;justify-content:center;box-shadow:3px 5px 9px #0002}.paper img{max-width:100%;max-height:100%;display:block}p{font-size:13px;letter-spacing:1px}</style></head><body><h1>Normal posters · 50 × 65 cm</h1><main>'''+cards+'</main><p>Original image files · no cropping · no distortion · unframed paper</p></body></html>')
print(json.dumps(stats,indent=2))
