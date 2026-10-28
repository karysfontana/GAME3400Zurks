from pathlib import Path
import numpy as np, math,json,struct
from PIL import Image,ImageDraw,ImageFont
from scipy.ndimage import gaussian_filter
R=Path(__file__).resolve().parents[1];T=R/'Textures';T.mkdir(exist_ok=True)
FONT='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf';BOLD='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
teal=(88,135,139);ink=(32,42,43);white=(244,242,232);gray=(167,177,176)
def textfit(d,xy,txt,maxwidth,size,bold=False):
 while True:
  f=ImageFont.truetype(BOLD if bold else FONT,size)
  if d.textbbox((0,0),txt,font=f)[2]<=maxwidth:break
  size-=1
 d.text(xy,txt,fill=ink,font=f)
def label(kind):
 if kind=='Carton_Print':
  im=Image.new('RGB',(1024,580),white);d=ImageDraw.Draw(im)
  d.polygon([(0,420),(1024,350),(1024,580),(0,580)],fill=gray)
  d.polygon([(0,443),(1024,373),(1024,580),(0,580)],fill=teal)
  textfit(d,(65,39),'Rx',180,47)
  textfit(d,(65,114),'ANTIDEPRESSANT',900,76,True)
  textfit(d,(69,214),'TABLETS',900,57)
  d.line([(70,293),(915,293)],fill=gray,width=4)
  textfit(d,(70,312),'PRESCRIPTION ONLY',850,27)
 elif kind=='Carton_Side':
  im=Image.new('RGB',(512,1024),white);d=ImageDraw.Draw(im)
  d.polygon([(0,740),(512,660),(512,1024),(0,1024)],fill=gray)
  d.polygon([(0,769),(512,689),(512,1024),(0,1024)],fill=teal)
  textfit(d,(48,85),'Rx',400,50)
  for yy,ww in [(200,370),(239,320),(278,350),(410,370),(449,280)]:d.rectangle((48,yy,48+ww,yy+7),fill=gray)
 else:
  im=Image.new('RGB',(2048,640),white);d=ImageDraw.Draw(im)
  d.rectangle((0,492,2048,513),fill=gray);d.rectangle((0,524,2048,640),fill=teal)
  textfit(d,(720,35),'Rx',600,50)
  textfit(d,(720,120),'ANTIDEPRESSANT',610,55,True)
  textfit(d,(720,208),'TABLETS',610,60)
  d.line((720,305,1328,305),fill=gray,width=5)
  textfit(d,(720,329),'PRESCRIPTION ONLY',610,30)
  for yy,ww in [(409,580),(444,480)]:d.rectangle((720,yy,720+ww,yy+6),fill=gray)
  for xx in [115,1560]:
   for yy,ww in [(175,330),(219,290),(263,315),(350,330),(394,250)]:d.rectangle((xx,yy,xx+ww,yy+7),fill=gray)
 return im
textures={};rng=np.random.default_rng(4)
for name in ['Carton_Print','Carton_Side','Bottle_Label','White_Plastic']:
 im=label(name) if name!='White_Plastic' else Image.new('RGB',(512,512),(239,239,234))
 a=np.asarray(im).astype(float);h,w=a.shape[:2];noise=rng.normal(0,1,(h,w));a=np.clip(a+noise[:,:,None]*(.55 if name!='White_Plastic' else .15),0,255).astype('uint8')
 Image.fromarray(a).save(T/(name+'_BaseColor.png'));textures[name]=a
 smooth=gaussian_filter(noise,1,mode='wrap');gy,gx=np.gradient(smooth);normal=np.stack([-gx*.035,gy*.035,np.ones_like(gx)],-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
 Image.fromarray(((normal*.5+.5)*255).astype('uint8')).save(T/(name+'_Normal.png'))
 rough=np.clip((205 if name!='White_Plastic' else 105)+noise*.7,0,255).astype('uint8');Image.fromarray(rough).save(T/(name+'_Roughness.png'))
 ms=np.zeros((h,w,4),dtype='uint8');ms[:,:,3]=255-rough;Image.fromarray(ms).save(T/(name+'_MetallicSmoothness.png'))
 orm=np.zeros((h,w,3),dtype='uint8');orm[:,:,0]=255;orm[:,:,1]=rough;Image.fromarray(orm).save(T/(name+'_ORM.png'))
parts=[]
def add(name,v,f,uv,mat):
 v=np.array(v,float);f=np.array(f,int);n=np.zeros_like(v);fn=np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]])
 for j in range(3):np.add.at(n,f[:,j],fn)
 n/=np.maximum(np.linalg.norm(n,axis=1,keepdims=True),1e-12)
 parts.append(dict(name=name,v=v,f=f,uv=np.array(uv,float),n=n,mat=mat))
def carton():
 global parts
 parts=[];h=np.array([.115,.065,.030])/2;r=.0007
 for axis in range(3):
  for sign in [-1,1]:
   a=(axis+1)%3;b=(axis+2)%3
   va=[-h[a],-h[a]+r,0,h[a]-r,h[a]];vb=[-h[b],-h[b]+r,0,h[b]-r,h[b]]
   verts=[];uv=[];faces=[]
   for u in va:
    for w in vb:
     p=np.zeros(3);p[axis]=sign*h[axis];p[a]=u;p[b]=w;q=np.clip(p,-h+r,h-r);delta=p-q;p=q+delta/np.linalg.norm(delta)*r
     verts.append(p+[0,h[1],0])
     if axis==2:uv.append(((p[0]/h[0]*sign+1)/2,(p[1]/h[1]+1)/2))
     elif axis==0:uv.append(((p[2]/h[2]+1)/2,(p[1]/h[1]+1)/2))
     else:uv.append(((p[0]/h[0]+1)/2,.08))
   for i in range(4):
    for j in range(4):
     k=i*5+j;ff=[(k,k+5,k+6),(k,k+6,k+1)];faces+=ff if sign>0 else [f[::-1] for f in ff]
   add(('Carton_Print' if axis==2 else 'Carton_Side')+f'_{axis}_{sign}',verts,faces,uv,'Carton_Print' if axis==2 else 'Carton_Side')
 return parts.copy()
def lathe(name,profile,mat,N=96,ribs=False,closed=True):
 vs=[];uv=[];fs=[]
 for j,(height,rad) in enumerate(profile):
  for i in range(N+1):
   theta=-math.pi+i/N*2*math.pi;rr=rad
   if ribs and j not in [0,len(profile)-1]:rr+=.00055*(.5+.5*math.cos(theta*64))
   vs.append((rr*math.sin(theta),height,rr*math.cos(theta)));uv.append((i/N,j/(len(profile)-1)))
 for j in range(len(profile)-1):
  for i in range(N):
   k=j*(N+1)+i;fs.extend([(k,k+1,k+N+2),(k,k+N+2,k+N+1)])
 if closed:
  for row,up in [(0,False),(len(profile)-1,True)]:
   c=len(vs);vs.append((0,profile[row][0],0));uv.append((.5,.5));start=row*(N+1)
   for i in range(N):fs.append((c,start+i,start+i+1) if up else (c,start+i+1,start+i))
 add(name,vs,fs,uv,mat)
def bottle():
 global parts
 parts=[]
 lathe('White_Plastic_Body',[(0,.019),(.001,.021),(.003,.0225),(.006,.0228),(.056,.0228),(.060,.0225),(.064,.0215),(.068,.019),(.071,.016),(.076,.016)],'White_Plastic')
 lathe('Bottle_Label',[(.012,.02295),(.056,.02295)],'Bottle_Label',closed=False)
 lathe('White_Plastic_Neck_Ring',[(.072,.016),(.0725,.018),(.074,.018),(.0745,.016)],'White_Plastic')
 lathe('White_Plastic_Ribbed_Cap',[(.074,.021),(.075,.022),(.077,.022),(.089,.022),(.091,.0218),(.092,.0205)],'White_Plastic',N=256,ribs=True)
 return parts.copy()
def export(name,objs):
 lines=['# Meters; Y up; floor-center pivot.','mtllib Medication_Materials.mtl'];off=1
 for p in objs:
  lines+=['o '+p['name'],'usemtl '+p['mat'],'s 1']
  lines+=['v %.8f %.8f %.8f'%tuple(v) for v in p['v']]
  lines+=['vt %.8f %.8f'%tuple(v) for v in p['uv']]
  lines+=['vn %.8f %.8f %.8f'%tuple(v) for v in p['n']]
  lines+=['f '+' '.join(f'{i+off}/{i+off}/{i+off}' for i in f) for f in p['f']];off+=len(p['v'])
 (R/(name+'.obj')).write_text('\n'.join(lines))
 blob=bytearray();views=[];acc=[]
 def data(b,target=None):
  blob.extend(b'\0'*((-len(blob))%4));v={'buffer':0,'byteOffset':len(blob),'byteLength':len(b)}
  if target:v['target']=target
  blob.extend(b);views.append(v);return len(views)-1
 def arr(a,typ,comp,target):
  a=np.array(a,dtype='<f4' if comp==5126 else '<u4');idx=data(a.tobytes(),target);it={'bufferView':idx,'componentType':comp,'count':len(a),'type':typ}
  if typ=='VEC3':it.update(min=a.min(0).tolist(),max=a.max(0).tolist())
  acc.append(it);return len(acc)-1
 used=list(dict.fromkeys(p['mat'] for p in objs));materials=[];images=[];tex=[]
 for mat in used:
  ids=[]
  for ext in ['BaseColor','Normal','ORM']:
   idx=data((T/(mat+'_'+ext+'.png')).read_bytes());images.append({'bufferView':idx,'mimeType':'image/png'});tex.append({'source':len(images)-1,'sampler':0});ids.append(len(tex)-1)
  materials.append({'name':mat,'pbrMetallicRoughness':{'baseColorTexture':{'index':ids[0]},'metallicRoughnessTexture':{'index':ids[2]},'metallicFactor':0,'roughnessFactor':1},'normalTexture':{'index':ids[1],'scale':.4}})
 meshes=[];nodes=[]
 for p in objs:
  attr={'POSITION':arr(p['v'],'VEC3',5126,34962),'NORMAL':arr(p['n'],'VEC3',5126,34962),'TEXCOORD_0':arr(p['uv']*[1,-1]+[0,1],'VEC2',5126,34962)}
  idx=arr(p['f'].ravel(),'SCALAR',5125,34963);meshes.append({'name':p['name'],'primitives':[{'attributes':attr,'indices':idx,'material':used.index(p['mat'])}]});nodes.append({'mesh':len(meshes)-1,'name':p['name']})
 doc={'asset':{'version':'2.0','generator':'Medication prop builder'},'scene':0,'scenes':[{'nodes':list(range(len(nodes)))}],'nodes':nodes,'meshes':meshes,'materials':materials,'images':images,'textures':tex,'samplers':[{'magFilter':9729,'minFilter':9987,'wrapS':33071,'wrapT':33071}],'buffers':[{'byteLength':len(blob)}],'bufferViews':views,'accessors':acc}
 js=json.dumps(doc,separators=(',',':')).encode();js+=b' '*((-len(js))%4);blob+=b'\0'*((-len(blob))%4)
 (R/(name+'.glb')).write_bytes(struct.pack('<III',0x46546c67,2,28+len(js)+len(blob))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(blob),0x004e4942)+blob)
 return {'objects':len(objs),'triangles':sum(len(p['f']) for p in objs),'vertices':sum(len(p['v']) for p in objs),'bounds_m':[np.vstack([p['v'] for p in objs]).min(0).tolist(),np.vstack([p['v'] for p in objs]).max(0).tolist()]}

def render(objs):
 W,H=1500,850;img=np.full((H,W,3),242.,float);dep=np.full((H,W),1e9)
 eye=np.array([.10,.11,.32]);target=np.array([0,.045,0]);forward=(target-eye);forward/=np.linalg.norm(forward);right=np.cross(forward,[0,1,0]);right/=np.linalg.norm(right);up=np.cross(right,forward);scale=5600
 light=np.array([-.4,1,1.5]);light/=np.linalg.norm(light)
 for p in objs:
  d=p['v']-target;pr=np.stack([d@right*scale+W/2,-d@up*scale+H/2,d@forward],-1);tex=textures[p['mat']]
  for face in p['f']:
   tri=pr[face];a,b,c=tri;x0=max(0,int(tri[:,0].min()));x1=min(W-1,int(np.ceil(tri[:,0].max())));y0=max(0,int(tri[:,1].min()));y1=min(H-1,int(np.ceil(tri[:,1].max())))
   if x0>x1 or y0>y1:continue
   den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
   if abs(den)<1e-12:continue
   xx,yy=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5);w0=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den;w1=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den;w2=1-w0-w1
   z=w0*a[2]+w1*b[2]+w2*c[2];view=dep[y0:y1+1,x0:x1+1];mask=(w0>=-1e-7)&(w1>=-1e-7)&(w2>=-1e-7)&(z<view)
   if not mask.any():continue
   weights=np.stack([w0[mask],w1[mask],w2[mask]],-1);uv=weights@p['uv'][face];ns=weights@p['n'][face];ns/=np.maximum(np.linalg.norm(ns,axis=1,keepdims=True),1e-9)
   tx=np.clip((uv[:,0]*(tex.shape[1]-1)).astype(int),0,tex.shape[1]-1);ty=np.clip(((1-uv[:,1])*(tex.shape[0]-1)).astype(int),0,tex.shape[0]-1)
   shade=.65+.35*np.maximum(0,ns@light)
   img[y0:y1+1,x0:x1+1][mask]=tex[ty,tx]*shade[:,None];view[mask]=z[mask]
 im=Image.fromarray(np.clip(img,0,255).astype('uint8'));d=ImageDraw.Draw(im);font=ImageFont.truetype(FONT,22)
 d.text((750,805),'ACTUAL MODELS  /  HORIZONTAL BOX + BOTTLE',font=font,fill=(65,65,65),anchor='mm');im.save(R/'Actual_Model_Preview.png')

if __name__=='__main__':
 a=carton();b=bottle();stats={}
 stats['Medication_Box_Horizontal']=export('Medication_Box_Horizontal',a);stats['Medication_Bottle']=export('Medication_Bottle',b)
 mtl=[]
 for mat in textures:mtl+=['newmtl '+mat,'Kd 1 1 1','Ks 0.05 0.05 0.05','Ns 20','d 1','illum 2','map_Kd Textures/'+mat+'_BaseColor.png','']
 (R/'Medication_Materials.mtl').write_text('\n'.join(mtl));(R/'Model_Info.json').write_text(json.dumps(stats,indent=2))
 for p in a:p['v']=p['v']+[-.046,0,0]
 for p in b:p['v']=p['v']+[.066,0,0]
 render(a+b);print(json.dumps(stats,indent=2))
