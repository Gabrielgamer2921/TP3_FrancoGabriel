import numpy as np, wave, os, sys
SR=44100
OUT=sys.argv[1]
rng=np.random.default_rng(11)

def T(d): return np.arange(int(SR*d))/SR
def noise(d): return rng.standard_normal(int(SR*d))
def band(x, lo=None, hi=None, slope=0.25):
    n=len(x); X=np.fft.rfft(x); f=np.fft.rfftfreq(n,1/SR); g=np.ones_like(f)
    if lo: g*=1/(1+np.exp(-(np.log2(np.maximum(f,1)/lo))/slope))
    if hi: g*=1/(1+np.exp((np.log2(np.maximum(f,1)/hi))/slope))
    return np.fft.irfft(X*g,n)
def sat(x, drive): return np.tanh(x*drive)/np.tanh(drive)
def norm(x): return x/(np.max(np.abs(x))+1e-9)
def conv(x, ir):
    n=len(x)+len(ir); return np.fft.irfft(np.fft.rfft(x,n)*np.fft.rfft(ir,n),n)
def tail_ir(length, cutoff, predelay=0.03):
    t=T(length); ir=band(noise(length),150,cutoff)*np.exp(-t/(length/6.0))
    ir*=np.clip((t-predelay)/0.04,0,1); return ir/np.sqrt(np.sum(ir**2)+1e-9)
def place(base, x, time, gain=1.0):
    i=int(time*SR); end=min(len(base), i+len(x))
    if end>i: base[i:end]+=x[:end-i]*gain
    return base
def save(name, x, peak):
    x=x-np.mean(x); x=norm(x)*peak
    fade=int(SR*0.02); x[-fade:]*=np.linspace(1,0,fade)
    path=os.path.join(OUT,name+'.wav'); d=(np.clip(x,-1,1)*32767).astype(np.int16)
    with wave.open(path,'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(d.tobytes())

ECHOES=[(0.11,0.32,3200),(0.24,0.2,2400),(0.39,0.13,1800),(0.57,0.08,1300),(0.8,0.05,1000)]

def gunshot(length, crack_ms, crack_hp, boom_f0, boom_f1, boom_decay, rumble_cut, body_decay, drive, bright, tail_len, tail_mix, extra_low=1.0):
    n=int(SR*length); t=np.arange(n)/SR
    crack=band(noise(length),crack_hp,16000)*np.exp(-t/(crack_ms/1000.0))
    crack[:20]*=np.linspace(0,1,20)
    freq=boom_f1+(boom_f0-boom_f1)*np.exp(-t/0.012)
    boom=np.sin(2*np.pi*np.cumsum(freq)/SR)*np.exp(-t/boom_decay)*(1-np.exp(-t/0.0007))
    rumble=band(noise(length),25,rumble_cut)*np.exp(-t/(boom_decay*2.2))*(1-np.exp(-t/0.002))
    body=band(noise(length),300,3500)*np.exp(-t/body_decay)
    dry=norm(crack)*1.0+norm(boom)*1.3*extra_low+norm(rumble)*0.8*extra_low+norm(body)*0.5
    dry=sat(norm(dry),drive)
    dry=band(dry,None,bright,0.3)
    out=dry.copy()
    for delay,gain,cut in ECHOES:
        e=band(dry,120,cut,0.3)*gain*rng.uniform(0.85,1.1); place(out,e,delay+rng.uniform(-0.01,0.01))
    tail=conv(dry,tail_ir(tail_len,2600))[:n]
    out=out+tail*tail_mix
    return out

for i in range(3):
    save(f'Revolver_Disparo_{i+1}', gunshot(1.7, 3.5+0.5*i, 900, 190-8*i, 58, 0.065+0.005*i, 220, 0.028, 3.2, 9500, 1.3, 0.22), 0.95)
    save(f'Winchester_Disparo_{i+1}', gunshot(2.3, 5.0+0.6*i, 1200, 160-6*i, 48, 0.085+0.006*i, 200, 0.035, 4.2, 10500, 1.8, 0.28), 0.95)
for i in range(2):
    save(f'Escopeta_Disparo_{i+1}', gunshot(2.5, 4.5, 700, 130-8*i, 38, 0.13, 160, 0.06, 3.6, 7000, 2.0, 0.32, 1.25), 0.95)

def modes(freqs, decays, amps, length):
    t=T(length); out=np.zeros(len(t))
    for f,d,a in zip(freqs,decays,amps):
        f*=rng.uniform(0.97,1.03); out+=a*np.sin(2*np.pi*f*t+rng.uniform(0,6.28))*np.exp(-t/d)
    return out*(1-np.exp(-t/0.0004))
def hit(length, lo, hi, decay):
    t=T(length); return band(noise(length),lo,hi)*np.exp(-t/decay)
def click(length=0.12, base=3000, ring=0.012, gain=1.0):
    m=modes([base, base*1.73, base*2.61, base*3.9],[ring, ring*0.7, ring*0.5, ring*0.35],[1,0.6,0.4,0.25],length)
    return (norm(hit(length,1500,12000,0.0015))*0.9+norm(m)*0.7)*gain
def clack(length=0.2, base=950, ring=0.035, gain=1.0):
    m=modes([base, base*1.74, base*2.62, base*4.1, base*5.9],[ring, ring*0.8, ring*0.6, ring*0.4, ring*0.3],[1,0.7,0.5,0.35,0.2],length)
    thunk=np.sin(2*np.pi*170*T(length))*np.exp(-T(length)/0.02)
    return (norm(hit(length,700,9000,0.003))*0.8+norm(m)*0.8+thunk*0.5)*gain
def scrape(length, lo=1800, hi=7000, gain=0.4):
    t=T(length); jitter=np.abs(band(noise(length),None,120)); jitter=norm(jitter)
    shape=np.sin(np.linspace(0,np.pi,len(t)))**0.7
    return norm(band(noise(length),lo,hi))*jitter*shape*gain
def room(x, mix=0.12):
    y=conv(x,tail_ir(0.35,4500,0.01))[:len(x)]; return x+y*mix

x=np.zeros(int(SR*0.35)); place(x,click(0.1,3400,0.008,0.6),0)
for k,tm in enumerate([0.03,0.045,0.06]): place(x,click(0.05,5200+300*k,0.004,0.18),tm)
place(x,click(0.15,2600,0.016,1.0),0.085); save('Revolver_Martillo', room(x), 0.75)

x=np.zeros(int(SR*0.6))
place(x,clack(0.25,880,0.04,1.0),0); place(x,scrape(0.11,1500,6500,0.45),0.02); place(x,click(0.08,4200,0.006,0.3),0.09)
place(x,scrape(0.09,1600,7000,0.4),0.22); place(x,clack(0.28,1020,0.045,1.15),0.31); place(x,click(0.1,6000,0.01,0.15),0.33)
save('Winchester_Palanca', room(x,0.15), 0.85)

x=np.zeros(int(SR*0.3)); place(x,clack(0.2,1400,0.025,0.7),0); place(x,click(0.1,4800,0.008,0.3),0.015); save('Recarga_Abrir', room(x), 0.7)
x=np.zeros(int(SR*0.35)); place(x,scrape(0.075,2200,8000,0.5),0); place(x,clack(0.18,1250,0.018,0.65),0.07); place(x,click(0.08,5600,0.005,0.25),0.085); save('Recarga_Bala', room(x), 0.7)
x=np.zeros(int(SR*0.35)); place(x,click(0.1,3800,0.01,0.6),0); place(x,clack(0.2,1150,0.03,0.8),0.035); save('Recarga_Cerrar', room(x), 0.72)
x=np.zeros(int(SR*0.3)); place(x,click(0.15,2900,0.012,1.0),0); t=T(0.15); place(x,np.sin(2*np.pi*150*t)*np.exp(-t/0.018)*0.35,0.002); save('Arma_Vacia', room(x), 0.7)
print('ok')
