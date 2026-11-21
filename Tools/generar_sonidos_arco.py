import numpy as np, wave, os, sys
SR=44100; OUT=sys.argv[1]; rng=np.random.default_rng(23)
def T(d): return np.arange(int(SR*d))/SR
def noise(d): return rng.standard_normal(int(SR*d))
def band(x, lo=None, hi=None, slope=0.25):
    n=len(x); X=np.fft.rfft(x); f=np.fft.rfftfreq(n,1/SR); g=np.ones_like(f)
    if lo: g*=1/(1+np.exp(-(np.log2(np.maximum(f,1)/lo))/slope))
    if hi: g*=1/(1+np.exp((np.log2(np.maximum(f,1)/hi))/slope))
    return np.fft.irfft(X*g,n)
def norm(x): return x/(np.max(np.abs(x))+1e-9)
def place(b,x,t,g=1.0):
    i=int(t*SR); e=min(len(b),i+len(x)); b[i:e]+=x[:e-i]*g; return b
def save(name,x,peak):
    x=x-np.mean(x); x=norm(x)*peak; f=int(SR*0.02); x[-f:]*=np.linspace(1,0,f)
    d=(np.clip(x,-1,1)*32767).astype(np.int16)
    with wave.open(os.path.join(OUT,name+'.wav'),'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(d.tobytes())
def room(x,mix=0.12,length=0.35):
    t=T(length); ir=band(noise(length),200,5000)*np.exp(-t/(length/6)); ir/=np.sqrt(np.sum(ir**2))
    n=len(x)+len(ir); y=np.fft.irfft(np.fft.rfft(x,n)*np.fft.rfft(ir,n),n)[:len(x)]; return x+y*mix
os.makedirs(OUT,exist_ok=True)
for i in range(3):
    d=0.9; t=T(d); f0=95+8*i
    pluck=sum((1/k)*np.sin(2*np.pi*f0*k*t*(1+0.002*k))*np.exp(-t/(0.18/k**0.5)) for k in range(1,7))
    thump=band(noise(d),60,500)*np.exp(-t/0.03)
    snap=band(noise(d),1500,9000)*np.exp(-t/0.004)
    whoosh=band(noise(d),400,3000)*np.exp(-((t-0.08)/0.06)**2)*0.5
    save(f'Arco_Disparo_{i+1}', room(norm(pluck)*0.8+norm(thump)*0.9+norm(snap)*0.5+whoosh,0.15), 0.85)
d=0.8; t=T(d)
creak=np.zeros(len(t))
for k in range(26):
    tick=band(noise(0.02),300,2500)*np.exp(-T(0.02)/0.004)*rng.uniform(0.3,1.0)
    place(creak,tick,(k/26)**1.3*0.7)
stretch=band(noise(d),150,900)*np.sin(np.linspace(0,np.pi*0.9,len(t)))*0.35
save('Arco_Tensar', room(creak+stretch,0.1), 0.55)
d=1.0; x=np.zeros(int(SR*d))
scrape=band(noise(0.35),1200,6000)*np.sin(np.linspace(0,np.pi,int(SR*0.35)))**0.6*np.abs(band(noise(0.35),None,60))
place(x,norm(scrape)*0.6,0.05)
tap=band(noise(0.06),800,6000)*np.exp(-T(0.06)/0.006)+np.sin(2*np.pi*420*T(0.06))*np.exp(-T(0.06)/0.015)*0.4
place(x,tap,0.48,0.9)
string=sum((1/k)*np.sin(2*np.pi*190*k*T(0.25))*np.exp(-T(0.25)/0.05) for k in range(1,4))
place(x,norm(string)*0.35,0.55)
save('Arco_Cargar', room(x,0.12), 0.6)
print('ok')
