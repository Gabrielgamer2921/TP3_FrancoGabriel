import numpy as np, wave, os, sys
SR = 44100
OUT = sys.argv[1]
rng = np.random.default_rng(7)

def t_(d): return np.arange(int(SR*d))/SR
def noise(d): return rng.standard_normal(int(SR*d))
def band(x, lo=None, hi=None, slope=0.15):
    n = len(x); X = np.fft.rfft(x); f = np.fft.rfftfreq(n, 1/SR); g = np.ones_like(f)
    if lo: g *= 1/(1+np.exp(-(np.log2(np.maximum(f,1)/lo))/slope))
    if hi: g *= 1/(1+np.exp((np.log2(np.maximum(f,1)/hi))/slope))
    return np.fft.irfft(X*g, n)
def env(d, attack=0.002, decay=0.1):
    t = t_(d); a = np.clip(t/attack,0,1) if attack>0 else np.ones_like(t)
    return a*np.exp(-t/decay)
def pad(x, d):
    n=int(SR*d); y=np.zeros(max(n,len(x))); y[:len(x)]=x; return y
def at(base, x, time, gain=1.0):
    i=int(time*SR); end=min(len(base), i+len(x)); base[i:end]+=x[:end-i]*gain; return base
def reverb(x, length=0.8, mix=0.3, damp=3000):
    ir = band(noise(length), 200, damp) * np.exp(-t_(length)/(length/5)); ir/=np.sqrt(np.sum(ir**2))+1e-9
    n=len(x)+len(ir); y=np.fft.irfft(np.fft.rfft(x,n)*np.fft.rfft(ir,n), n)[:len(x)+len(ir)]
    out=np.zeros(len(y)); out[:len(x)]=x; return out*(1-mix)+y*mix
def sine(f, d, decay=None, phase=0):
    t=t_(d); s=np.sin(2*np.pi*f*t+phase); return s*np.exp(-t/decay) if decay else s
def finish(x, peak=0.9, fade=0.01):
    x = x - np.mean(x); m=np.max(np.abs(x))+1e-9; x = x/m*peak
    nf=int(SR*fade); 
    if nf>0 and len(x)>nf: x[-nf:]*=np.linspace(1,0,nf)
    return x
def save(name, x, peak=0.9):
    path=os.path.join(OUT, name+'.wav'); os.makedirs(os.path.dirname(path), exist_ok=True)
    x=finish(x, peak); d=(np.clip(x,-1,1)*32767).astype(np.int16)
    with wave.open(path,'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(d.tobytes())

def gunshot(d, crack_decay, body_decay, body_hi, thump_f, thump_decay, rev_len, rev_mix, crack_gain=1.0, body_gain=1.0):
    crack = band(noise(d),900,9000)*env(d,0.0005,crack_decay)*crack_gain
    body = band(noise(d),120,body_hi)*env(d,0.001,body_decay)*body_gain
    thump = sine(thump_f*(1+0.3*np.exp(-t_(d)/0.02)), d, thump_decay)*1.2
    return reverb(crack+body+thump, rev_len, rev_mix)

for i in range(3):
    save(f'Armas/Revolver_Disparo_{i+1}', gunshot(0.6, 0.010+0.002*i, 0.07, 3000+300*i, 95-5*i, 0.05, 0.7, 0.28))
    save(f'Armas/Winchester_Disparo_{i+1}', gunshot(0.8, 0.014+0.002*i, 0.11, 2200+200*i, 70-4*i, 0.08, 1.1, 0.35, 1.2))
for i in range(2):
    save(f'Armas/Escopeta_Disparo_{i+1}', gunshot(0.9, 0.012, 0.17, 2600, 60-5*i, 0.12, 1.2, 0.38, 0.8, 1.4))

def click(f=3200, decay=0.012, d=0.2, lo=1500, hi=9000):
    return band(noise(d),lo,hi)*env(d,0.0003,0.004) + sine(f,d,decay)*0.5
def metal(f, d=0.2): return sum(sine(f*k, d, 0.03/k)*(1/k) for k in (1,2.7,4.1))

x=np.zeros(int(SR*0.25)); at(x, click(2600), 0); at(x, click(3400,0.01), 0.07, 0.8); save('Armas/Revolver_Martillo', reverb(x,0.2,0.1))
x=np.zeros(int(SR*0.45)); at(x, click(1800,0.02)+metal(900)*0.6, 0); at(x, band(noise(0.12),1500,6000)*env(0.12,0.02,0.05)*0.35, 0.05); at(x, click(2200,0.02)+metal(1100)*0.7, 0.24, 1.1); save('Armas/Winchester_Palanca', reverb(x,0.25,0.12))
x=np.zeros(int(SR*0.3)); at(x, band(noise(0.1),1000,5000)*env(0.1,0.01,0.03)*0.4, 0); at(x, click(2900,0.015), 0.08); save('Armas/Recarga_Bala', x)
x=np.zeros(int(SR*0.2)); at(x, click(2000,0.02), 0); at(x, metal(700,0.15)*0.4, 0.01); save('Armas/Recarga_Abrir', x)
x=np.zeros(int(SR*0.2)); at(x, click(2400,0.02), 0); at(x, click(1700,0.02), 0.04, 0.6); save('Armas/Recarga_Cerrar', x)
save('Armas/Arma_Vacia', band(click(1400,0.01,0.12),300,2500))
x=np.zeros(int(SR*0.35)); w=band(noise(0.3),400,3000)*np.sin(np.linspace(0,np.pi,int(SR*0.3)))**2; at(x,w,0); save('Armas/Culatazo_Swing', x, 0.6)
x=band(noise(0.3),60,700)*env(0.3,0.002,0.05)+sine(70,0.3,0.06); save('Armas/Culatazo_Golpe', x)

def loop_fade(x, cross):
    n=int(SR*cross); a=x[:n].copy(); b=x[-n:].copy(); fade=np.linspace(0,1,n)
    y=x[:-n].copy(); y[:n]=a*fade+b*(1-fade); return y
d=2.0; hiss=band(noise(d),2500,9000)*0.35
pops=np.zeros(int(SR*d))
for k in range(60):
    p=band(noise(0.01),2000,10000)*env(0.01,0.0002,0.002)*rng.uniform(0.3,1.0); at(pops,p,rng.uniform(0,d-0.02))
save('Explosivos/Mecha_Loop', loop_fade(hiss+pops,0.3), 0.7)
x=np.zeros(int(SR*0.6)); s=band(noise(0.25),1500,8000)*env(0.25,0.005,0.06); at(x,s,0); at(x,band(noise(0.45),800,6000)*env(0.45,0.05,0.25)*0.5,0.05); save('Explosivos/Fosforo', x, 0.7)
for i in range(2):
    d=3.0; rumble=band(noise(d),30,350)*env(d,0.004,0.6+0.2*i); crack=band(noise(d),800,9000)*env(d,0.0005,0.03)
    thump=sine(42+6*i,d,0.3)*1.5; debris=np.zeros(int(SR*d))
    for k in range(25): at(debris, band(noise(0.05),1500,7000)*env(0.05,0.001,0.01)*rng.uniform(0.05,0.2), rng.uniform(0.2,1.8))
    save(f'Explosivos/Explosion_{i+1}', reverb(rumble*1.4+crack*0.8+thump+debris,1.6,0.35))
x=np.zeros(int(SR*0.4)); at(x, band(noise(0.3),300,2500)*np.sin(np.linspace(0,np.pi,int(SR*0.3))), 0); save('Explosivos/Lanzar', x, 0.5)
x=np.zeros(int(SR*1.4))
for k in range(6): at(x, band(noise(0.08),400,5000)*env(0.08,0.001,0.02)*rng.uniform(0.4,1), rng.uniform(0,0.4))
at(x, band(noise(1.0),100,1500)*env(1.0,0.005,0.2)*0.6, 0); save('Explosivos/Madera_Rota', reverb(x,0.6,0.2))

def bell(f0, d=3.5):
    parts=[(1,1.0,1.6),(2.0,0.6,1.1),(2.42,0.5,0.9),(3.0,0.35,0.7),(4.13,0.25,0.5),(5.4,0.15,0.35)]
    return sum(sine(f0*r,d,dec)*a for r,a,dec in parts)+band(noise(d),2000,8000)*env(d,0.0005,0.01)*0.3
x=np.zeros(int(SR*4.5))
for k,tm in enumerate([0,0.55,1.1,1.65]): at(x, bell(520,2.8), tm, 1-0.1*k)
save('Mundo/Campana_Alarma', reverb(x,1.2,0.25))

for i in range(5):
    d=0.22; crunch=np.zeros(int(SR*d))
    for k in range(30): at(crunch, band(noise(0.004),1500,7000)*rng.uniform(0.2,1), rng.uniform(0,0.08))
    soft=band(noise(d),150,1600)*env(d,0.01,0.05)
    save(f'Jugador/Paso_Arena_{i+1}', (soft+crunch*0.25*env(d,0.005,0.06)), 0.5)
save('Jugador/Aterrizaje', band(noise(0.4),60,900)*env(0.4,0.003,0.08)+sine(55,0.4,0.08)*0.8, 0.7)
x=np.zeros(int(SR*0.9)); w=band(noise(0.7),500,6000)*np.linspace(0.2,1,int(SR*0.7))*env(0.7,0.01,0.5); at(x,w,0.05); save('Jugador/Vendaje', x, 0.6)
save('Jugador/Dano_Recibido', band(noise(0.35),80,1200)*env(0.35,0.002,0.06)+sine(65,0.35,0.07), 0.8)
x=np.zeros(int(SR*0.9)); at(x, sine(48,0.2,0.05)+band(noise(0.2),30,200)*env(0.2,0.002,0.04),0); at(x, sine(44,0.2,0.04)*0.7,0.22); save('Jugador/Latido_Loop', x, 0.8)

for i in range(3):
    d=0.3; x=band(noise(d),250,4500)*env(d,0.0008,0.035)+band(noise(d),80,400)*env(d,0.002,0.06)*0.7
    save(f'Impactos/Tierra_{i+1}', x, 0.7)
for i in range(2):
    d=0.3; x=sine(230+40*i,d,0.07)+sine(610+60*i,d,0.04)*0.6+band(noise(d),1500,7000)*env(d,0.0005,0.006)
    save(f'Impactos/Madera_{i+1}', x, 0.7)
for i in range(3):
    d=0.3; x=band(noise(d),60,700)*env(d,0.002,0.06)+band(noise(d),400,1600)*env(d,0.002,0.03)*0.5+sine(80+10*i,d,0.05)*0.6
    save(f'Impactos/Carne_{i+1}', x, 0.8)

x=np.zeros(int(SR*0.5))
for k,tm in enumerate([0,0.06,0.13]): at(x, metal(2400+300*k,0.2)*0.6+click(3000,0.01), tm)
save('UI/Pickup_Municion', x, 0.5)
x=np.zeros(int(SR*0.4)); at(x, band(noise(0.3),600,5000)*np.sin(np.linspace(0,np.pi,int(SR*0.3)))**2, 0); save('UI/Pickup_Objeto', x, 0.5)
x=np.zeros(int(SR*1.2)); at(x, sine(880,1.0,0.4)*0.7+sine(1760,1.0,0.2)*0.2,0); at(x, sine(1320,1.0,0.5)*0.7+sine(2640,1.0,0.25)*0.2,0.12); save('UI/Pickup_Llave', x, 0.6)
save('UI/Hitmarker', sine(3000,0.05,0.012)+band(noise(0.05),3000,9000)*env(0.05,0.0005,0.004)*0.5, 0.5)
x=np.zeros(int(SR*0.25)); at(x, sine(1800,0.2,0.05),0); at(x, sine(110,0.2,0.05)*0.8,0); save('UI/Kill', x, 0.6)
save('UI/Headshot', sine(2200,0.4,0.12)+sine(3300,0.4,0.06)*0.4, 0.5)

d=1.2; t=t_(d); f=150+90*np.sin(np.linspace(0,np.pi*1.4,len(t)))
creak=np.sign(np.sin(2*np.pi*np.cumsum(f)/SR))*0.3; creak=band(creak*(0.6+0.4*rng.standard_normal(len(t))*0.2),150,2500)*np.sin(np.linspace(0,np.pi,len(t)))
save('Mundo/Puerta_Abrir', reverb(creak,0.5,0.2), 0.6)
x=np.zeros(int(SR*0.8)); at(x, band(noise(0.5),50,800)*env(0.5,0.002,0.08)+sine(75,0.5,0.1)+sine(240,0.5,0.06)*0.4, 0); save('Mundo/Puerta_Cerrar', reverb(x,0.6,0.25), 0.8)
x=np.zeros(int(SR*0.5))
for tm in (0,0.09,0.17): at(x, metal(1500,0.2)*0.6+click(2200,0.015), tm)
save('Mundo/Puerta_Trabada', x, 0.6)

d=14.0; w=np.cumsum(rng.standard_normal(int(SR*d))); w=w-np.convolve(w,np.ones(2000)/2000,'same'); w=band(w,90,900)
t=t_(d); lfo=0.55+0.25*np.sin(2*np.pi*0.071*t)+0.2*np.sin(2*np.pi*0.13*t+1.3)
gust=band(noise(d),700,2200)*np.clip(np.sin(2*np.pi*0.045*t),0,1)**3*0.25
save('Mundo/Viento_Loop', loop_fade(w*lfo+gust*np.max(np.abs(w))*0.4,1.5), 0.6)
print('ok')
