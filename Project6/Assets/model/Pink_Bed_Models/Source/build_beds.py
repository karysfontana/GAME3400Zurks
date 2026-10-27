from pathlib import Path
import numpy as np, math, json, struct, zipfile
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import gaussian_filter
ROOT=Path(__file__).resolve().parents[1]
TEX=ROOT/'Textures'; TEX.mkdir(exist_ok=True)
rng=np.random.default_rng(23)
# All maps are mathematically generated, seamless material maps; no photo lighting baked in.
S=1024
Y,X=np.mgrid[0:S,0:S]/S
noise=gaussian_filter(rng.normal(size=(S,S)),2,mode='wrap');noise/=noise.std()
weave=(np.sin(X*math.tau*256)*np.sin(Y*math.tau*256))
def maps(name,col,height,rough,variation):
    rgb=np.clip(np.array(col)[None,None,:]+variation[:,:,None],0,255).astype('uint8')
    Image.fromarray(rgb).save(TEX/(name+'_BaseColor.png'))
    dx=(np.roll(height,-1,1)-np.roll(height,1,1))*2
    dy=(np.roll(height,-1,0)-np.roll(height,1,0))*2
    n=np.stack([-dx,dy,np.ones_like(dx)],-1);n/=np.linalg.norm(n,axis=-1,keepdims=True)
    Image.fromarray(((n*.5+.5)*255).astype('uint8')).save(TEX/(name+'_Normal.png'))
    rr=np.clip(rough+noise*2,0,255).astype('uint8')
    Image.fromarray(rr).save(TEX/(name+'_Roughness.png'))
    # Unity metallic workflow: RGB metallic=0, A smoothness=1-roughness.
    rgba=np.zeros((S,S,4),dtype='uint8');rgba[:,:,3]=255-rr
    Image.fromarray(rgba).save(TEX/(name+'_MetallicSmoothness.png'))
    return rgb
textures={}
for name,color in [('BlushFabric',(226,184,190)),('IvoryFabric',(236,230,218))]:
    textures[name]=maps(name,color,.08*weave+.03*noise,215,noise*1.4+weave*2)
# Directional oak-like fibers, broad grain and short pores.
warp=.015*np.sin(Y*math.tau*3)+.008*np.sin(Y*math.tau*7+np.sin(X*math.tau))
grain=np.sin((X+warp)*math.tau*43+1.8*np.sin(Y*math.tau*2))
fine=np.sin((X+warp*.5)*math.tau*181)
woodvar=grain*8+fine*3+noise*1.7+np.sin(X*math.tau*5+Y*math.tau)*7
textures['WarmWood']=maps('WarmWood',(137,103,73),.15*grain+.05*fine+.02*noise,175,woodvar)
# Quilt-specific albedo and normal stitching, aligned to the quilt mesh UVs.
du=np.minimum((X*9)%1,1-(X*9)%1)
dv=np.minimum((Y*7)%1,1-(Y*7)%1)
seam=np.maximum(np.exp(-(du/.023)**2),np.exp(-(dv/.023)**2))
stitch=seam*(.65+.35*np.sin((X*320+Y*320)*math.tau)**2)
textures['BlushQuilt']=maps('BlushQuilt',(226,184,190),.07*weave+.02*noise-.18*seam,216,noise*1.2+weave*1.2-stitch*14)
meshes=[]
def add(name,v,f,uv,mat):
    v=np.array(v,float);f=np.array(f,int);uv=np.array(uv,float)
    n=np.zeros_like(v);fn=np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]])
    for i in range(3):np.add.at(n,f[:,i],fn)
    n/=np.maximum(np.linalg.norm(n,axis=1,keepdims=True),1e-10)
    d=dict(name=name,v=v,f=f,uv=uv,n=n,mat=mat);meshes.append(d);return d

def grid_faces(nu,nv):
    f=[]
    for i in range(nu-1):
      for j in range(nv-1):
        a=i*nv+j;b=a+nv;f.extend([(a,b,b+1),(a,b+1,a+1)])
    return f

def box(name,center,dims,bevel,mat):
    h=np.array(dims)/2;r=min(bevel,min(h)*.95);vs=[];fs=[];us=[]
    # Rounded box has separate face normals/UV seams, physically bevelled edges.
    vals=[-1,-.93,-.8,0,.8,.93,1]
    for axis in range(3):
      for sign in [-1,1]:
        a=(axis+1)%3;b=(axis+2)%3;off=len(vs)
        for u in vals:
          for w in vals:
            p=np.zeros(3);p[axis]=sign*h[axis];p[a]=u*h[a];p[b]=w*h[b]
            q=np.clip(p,-h+r,h-r);d=p-q;p=q+d/np.linalg.norm(d)*r
            vs.append(p+center);us.append(((u+1)/2,(w+1)/2))
        ff=np.array(grid_faces(7,7))+off
        if sign<0:ff=ff[:,::-1]
        fs.extend(ff)
    return add(name,vs,fs,us,mat)

def cloth(name,kind,mat,messy=False):
    nu,nv=(105,99) if kind=='quilt' else (105,35)
    U,V=np.meshgrid(np.linspace(0,1,nu),np.linspace(0,1,nv),indexing='ij')
    sx=(U-.5)*2.46
    if kind=='quilt':
        sy=-.29+V*1.76
        if messy:
            sy+=.48*np.exp(-((sx-.4)/.65)**2)*(1-V)**2
            sy+=.10*np.sin(sx*7+V*12)*np.sin(np.pi*V)*np.exp(-V*2)
    else:
        sy=-.50+V*.43
        if messy:
            sy+=.20*np.sin(sx*3)+.17*np.cos(sx*5)*(V-.5)
    dx=np.maximum(abs(sx)-.87,0);dy=np.maximum(sy-.97,0)
    x=np.sign(sx)*(np.minimum(abs(sx),.87)+.075*(1-np.exp(-dx*15)))
    y=np.minimum(sy,.97)+.075*(1-np.exp(-dy*15))
    drop=np.sqrt(dx*dx+dy*dy)*.95
    z=.655-drop
    # Cloth wrinkles increase toward draped edge, no noisy displacement.
    edge=np.minimum(1,(dx+dy)*8)
    folds=(.002+.010*edge)*np.sin(sx*19+sy*6)+.002*np.sin(sx*33-sy*14)
    z+=folds
    if kind=='quilt':
        # Geometric quilting in a consistent UV grid; shallow seam channels.
        tileU=U*9;tileV=V*7
        puff=np.sin(np.pi*(tileU%1))**.7*np.sin(np.pi*(tileV%1))**.7
        z+=.013*puff
        if messy:
            env=np.exp(-((sy-.18)/.52)**2)*np.exp(-((sx-.20)/1.1)**4)
            z+=env*(.060+.075*(.5+.5*np.sin(sy*22+sx*7))+.08*np.exp(-((sx-.43)/.2)**2))
            x+=.025*env*(1-edge)*np.sin(sy*17+sx*6)
            y+=.045*env*np.sin(sy*21+sx*4)
            # Turned back upper edge, rising and curving toward the foot.
            t=np.clip(1-V/.14,0,1);z+=.11*np.sin(t*np.pi*.8)*(1+.35*np.sin(sx*6));y+=.09*t
    else:
        z+=.035+.035*np.sin(np.pi*V)**2
        if messy:
            env=np.maximum(0,1-(sx/1.3)**4)
            z+=env*(.05+.027*np.sin(sx*9+V*8)**2+.015*np.cos(sx*15-V*5)**2)
            y+=.04*np.sin(sx*9+V*5)
    vv=np.stack([x,y,z],-1).reshape(-1,3);uv=np.stack([U,V],-1).reshape(-1,2)
    f=np.array(grid_faces(nu,nv));count=len(vv)
    thickness=.018 if kind=='quilt' else .011
    bottom=vv.copy();bottom[:,2]-=thickness
    faces=list(map(tuple,f));faces.extend(map(tuple,(f+count)[:,::-1]))
    boundary=list(range(0,(nu-1)*nv+1,nv))+list(range((nu-1)*nv+1,nu*nv))+list(range(nu*nv-1-nv,nv-2,-nv))+list(range(nv-2,0,-1))
    for a,b in zip(boundary,boundary[1:]+boundary[:1]):faces.extend([(a,a+count,b+count),(a,b+count,b)])
    return add(name,np.vstack([vv,bottom]),faces,np.vstack([uv,uv]),mat)

def pillow(name,center,width,length,thick,tilt,rot,mat,seed):
    nu=35;nv=27
    U,V=np.meshgrid(np.linspace(-1,1,nu),np.linspace(-1,1,nv),indexing='ij')
    # Rounded rectangular silhouette and a softly stuffed, pinched-seam volume.
    xx=U*width/2*(1-.08*abs(V)**10);yy=V*length/2*(1-.08*abs(U)**10)
    fullness=np.maximum(0,(1-U*U)*(1-V*V))**.48
    zz=thick/2*(.11+.89*fullness)
    zz+=.004*np.sin(U*32+V*8+seed)*np.exp(-((abs(V)-.83)/.17)**2)*fullness
    v0=np.stack([xx,yy,zz],-1).reshape(-1,3);v1=v0.copy();v1[:,2]*=-1
    vs=np.vstack([v0,v1]);c=len(v0);fs=np.array(grid_faces(nu,nv));faces=list(map(tuple,fs));faces+=list(map(tuple,(fs+c)[:,::-1]))
    boundary=list(range(0,(nu-1)*nv+1,nv))+list(range((nu-1)*nv+1,nu*nv))+list(range(nu*nv-1-nv,nv-2,-nv))+list(range(nv-2,0,-1))
    for a,b in zip(boundary,boundary[1:]+boundary[:1]):faces.extend([(a,a+c,b+c),(a,b+c,b)])
    t=math.radians(-tilt);r=math.radians(rot)
    rx=np.array([[1,0,0],[0,math.cos(t),-math.sin(t)],[0,math.sin(t),math.cos(t)]])
    rz=np.array([[math.cos(r),-math.sin(r),0],[math.sin(r),math.cos(r),0],[0,0,1]])
    vs=vs@rx.T@rz.T+center
    uv=np.stack([(U+1)/2,(V+1)/2],-1).reshape(-1,2)
    return add(name,vs,faces,np.vstack([uv,uv]),mat)

def build(messy):
    global meshes
    meshes=[]
    for side in [-1,1]:
        box('Head_Post_'+str(side),(side*.97,-1.08,.74),(.11,.13,1.48),.012,'WarmWood')
        box('Head_Post_Cap_'+str(side),(side*.97,-1.08,1.47),(.16,.18,.055),.008,'WarmWood')
        box('Foot_Leg_'+str(side),(side*.85,.94,.20),(.12,.12,.40),.008,'WarmWood')
        box('Side_Rail_'+str(side),(side*.86,0,.35),(.09,2.08,.19),.008,'WarmWood')
    box('Head_Top_Rail',(0,-1.08,1.39),(1.89,.12,.16),.009,'WarmWood')
    box('Head_Lower_Rail',(0,-1.08,.63),(1.86,.10,.12),.006,'WarmWood')
    for i in range(11):
        box('Head_Plank_%02d'%i,(-.835+i*.167,-1.083,1.015),(.162,.064,.65),.005,'WarmWood')
    box('Foot_Rail',(0,.94,.35),(1.78,.10,.18),.008,'WarmWood')
    box('Mattress_Support',(0,0,.38),(1.78,2.0,.06),.008,'WarmWood')
    box('Mattress_and_Fitted_Sheet',(0,0,.51),(1.80,2.0,.235),.065,'IvoryFabric')
    if not messy:
        for i,xx in enumerate([-.44,.44]):
            pillow('Ivory_Pillow_'+str(i+1),(xx,-.77,.865),.83,.56,.20,65,(-2 if i==0 else 2),'IvoryFabric',i)
            pillow('Pink_Pillow_'+str(i+1),(xx,-.48,.82),.69,.46,.19,62,(-3 if i==0 else 3),'BlushFabric',i+3)
    else:
        pillow('Ivory_Pillow_1',(-.45,-.76,.85),.83,.56,.20,58,-9,'IvoryFabric',1)
        pillow('Ivory_Pillow_2',(.43,-.73,.83),.83,.56,.20,49,11,'IvoryFabric',2)
        pillow('Pink_Pillow_1',(-.43,-.39,.80),.69,.46,.19,53,-17,'BlushFabric',3)
        pillow('Pink_Pillow_2',(.48,-.41,.76),.69,.46,.19,18,17,'BlushFabric',4)
    cloth('Pink_Quilt','quilt','BlushQuilt',messy)
    cloth('Ivory_Folded_Blanket','blanket','IvoryFabric',messy)
    return meshes.copy()

def y_up(a):return a[:,[0,2,1]]*np.array([1,1,-1])

def export_obj(name,parts):
    out=['# Units: meters. Y up. Origin: floor at bed center.','mtllib Pink_Bed_Materials.mtl'];off=1
    for p in parts:
        out+=['o '+p['name'],'usemtl '+p['mat'],'s 1']
        out+=['v %.6f %.6f %.6f'%tuple(x) for x in y_up(p['v'])]
        out+=['vt %.6f %.6f'%tuple(x) for x in p['uv']]
        out+=['vn %.6f %.6f %.6f'%tuple(x) for x in y_up(p['n'])]
        out+=['f '+' '.join(f'{i+off}/{i+off}/{i+off}' for i in face) for face in p['f']]
        off+=len(p['v'])
    (ROOT/(name+'.obj')).write_text('\n'.join(out))

def export_glb(name,parts):
    blob=bytearray();views=[];access=[]
    def buffer(data,target=None):
        while len(blob)%4:blob.append(0)
        start=len(blob);blob.extend(data);v={'buffer':0,'byteOffset':start,'byteLength':len(data)}
        if target:v['target']=target
        views.append(v);return len(views)-1
    def arr(a,kind,ctype,target):
        a=np.ascontiguousarray(a,dtype=('<f4' if ctype==5126 else '<u4'));vi=buffer(a.tobytes(),target)
        ac={'bufferView':vi,'componentType':ctype,'count':len(a),'type':kind}
        if kind=='VEC3':ac.update(min=a.min(axis=0).tolist(),max=a.max(axis=0).tolist())
        access.append(ac);return len(access)-1
    mats=[];images=[];tex=[]
    for mat in textures:
        imgid=len(images);images.append({'bufferView':buffer((TEX/(mat+'_BaseColor.png')).read_bytes()),'mimeType':'image/png'})
        tex.append({'source':imgid,'sampler':0})
        nm=len(images);images.append({'bufferView':buffer((TEX/(mat+'_Normal.png')).read_bytes()),'mimeType':'image/png'});tex.append({'source':nm,'sampler':0})
        mats.append({'name':mat,'pbrMetallicRoughness':{'baseColorTexture':{'index':imgid},'metallicFactor':0,'roughnessFactor':.84 if mat!='WarmWood' else .68},'normalTexture':{'index':nm,'scale':.4}})
    nodes=[];gmeshes=[]
    for p in parts:
        attrs={'POSITION':arr(y_up(p['v']),'VEC3',5126,34962),'NORMAL':arr(y_up(p['n']),'VEC3',5126,34962),'TEXCOORD_0':arr(p['uv']*np.array([1,-1])+[0,1],'VEC2',5126,34962)}
        ind=arr(p['f'].reshape(-1),'SCALAR',5125,34963)
        gmeshes.append({'name':p['name'],'primitives':[{'attributes':attrs,'indices':ind,'material':list(textures).index(p['mat'])}]})
        nodes.append({'name':p['name'],'mesh':len(gmeshes)-1})
    doc={'asset':{'version':'2.0','generator':'Eco Pink Bed mesh builder'},'scene':0,'scenes':[{'nodes':list(range(len(nodes)))}],'nodes':nodes,'meshes':gmeshes,'materials':mats,'textures':tex,'images':images,'samplers':[{'magFilter':9729,'minFilter':9987,'wrapS':10497,'wrapT':10497}],'buffers':[{'byteLength':len(blob)}],'bufferViews':views,'accessors':access}
    js=json.dumps(doc,separators=(',',':')).encode();js+=b' '*((-len(js))%4);blob+=b'\x00'*((-len(blob))%4)
    total=12+8+len(js)+8+len(blob)
    (ROOT/(name+'.glb')).write_bytes(struct.pack('<III',0x46546c67,2,total)+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(blob),0x004e4942)+blob)

# Simple deterministic software renderer of the actual exported mesh geometry.
def render(parts,path):
    W=1100;H=1000
    eye=np.array([3.1,4.7,3.2]);target=np.array([0,0,.68]);forward=(target-eye);forward/=np.linalg.norm(forward)
    right=np.cross(forward,[0,0,1]);right/=np.linalg.norm(right);up=np.cross(right,forward)
    scale=310
    def proj(v):
        d=v-target;return np.stack([d@right*scale+W/2,-d@up*scale+H/2,d@forward],-1)
    zbuf=np.full((H,W),1e10);bg=np.zeros((H,W,3))+np.array([242,239,234]);img=bg.copy()
    # Ground contact shadow, rendered beneath model.
    xx,yy=np.meshgrid(np.arange(W),np.arange(H));sh=np.exp(-(((xx-W/2)/340)**2+((yy-710)/105)**2)*2)
    img-=sh[:,:,None]*24
    light=np.array([-3,0.3,6]);light/=np.linalg.norm(light)
    for p in parts:
      pr=proj(p['v']);tex=textures[p['mat']];uv=p['uv'];norm=p['n']
      for f in p['f']:
        tri=pr[f];x0=max(0,int(np.floor(tri[:,0].min())));x1=min(W-1,int(np.ceil(tri[:,0].max())))
        y0=max(0,int(np.floor(tri[:,1].min())));y1=min(H-1,int(np.ceil(tri[:,1].max())))
        if x0>x1 or y0>y1:continue
        a,b,c=tri
        den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
        if abs(den)<1e-10:continue
        gx,gy=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
        w0=((b[1]-c[1])*(gx-c[0])+(c[0]-b[0])*(gy-c[1]))/den
        w1=((c[1]-a[1])*(gx-c[0])+(a[0]-c[0])*(gy-c[1]))/den;w2=1-w0-w1
        z=w0*a[2]+w1*b[2]+w2*c[2];view=zbuf[y0:y1+1,x0:x1+1]
        mask=(w0>=-1e-6)&(w1>=-1e-6)&(w2>=-1e-6)&(z<view)
        if not mask.any():continue
        weights=np.stack([w0[mask],w1[mask],w2[mask]],-1)
        ns=weights@norm[f];ns/=np.maximum(1e-8,np.linalg.norm(ns,axis=1,keepdims=True))
        coords=weights@uv[f];tx=(coords[:,0]*1023).astype(int)%1024;ty=((1-coords[:,1])*1023).astype(int)%1024
        base=tex[ty,tx].astype(float)
        shade=.58+.39*np.maximum(0,ns@light)+.06*ns[:,2]
        img[y0:y1+1,x0:x1+1][mask]=np.clip(base*shade[:,None],0,255);view[mask]=z[mask]
    Image.fromarray(img.astype('uint8')).save(path)

if __name__=='__main__':
    mtl=[]
    for mat in textures:
        mtl+=['newmtl '+mat,'Ka 0.15 0.15 0.15','Kd 1 1 1','Ks 0.06 0.06 0.06','Ns 20','d 1','illum 2','map_Kd Textures/'+mat+'_BaseColor.png','']
    (ROOT/'Pink_Bed_Materials.mtl').write_text('\n'.join(mtl))
    stats={}
    for messy,name in [(False,'Bed_Neat'),(True,'Bed_Messy')]:
        parts=build(messy);export_obj(name,parts);export_glb(name,parts)
        np.savez_compressed(ROOT/'Source'/(name+'_mesh.npz'),**{str(i)+'_'+k:p[k] for i,p in enumerate(parts) for k in ['v','f','uv','n']})
        stats[name]={'objects':len(parts),'vertices':sum(len(p['v']) for p in parts),'triangles':sum(len(p['f']) for p in parts),'bounds_m':{'min':np.vstack([p['v'] for p in parts]).min(axis=0).tolist(),'max':np.vstack([p['v'] for p in parts]).max(axis=0).tolist()}}
        print(name,stats[name],flush=True)
        render(parts,ROOT/(name+'_Preview.png'));print('Rendered',name,flush=True)
    (ROOT/'Model_Stats.json').write_text(json.dumps(stats,indent=2))
