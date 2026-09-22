const canvas = document.querySelector("#game");
const ctx = canvas.getContext("2d");
const ui = {
  start: document.querySelector("#startScreen"), startButton: document.querySelector("#startButton"),
  finish: document.querySelector("#finishScreen"), restart: document.querySelector("#restartButton"),
  altitude: document.querySelector("#altitude"), best: document.querySelector("#best"), shots: document.querySelector("#shots"),
  fill: document.querySelector("#altitudeFill"), bullet: document.querySelector("#bulletStatus"), bulletText: document.querySelector("#bulletText"),
  crosshair: document.querySelector("#crosshair"), tip: document.querySelector("#controlsTip"), section: document.querySelector("#sectionTag"),
  soundButton: document.querySelector("#soundButton"), soundLabel: document.querySelector("#soundLabel")
};

let W, H, scale, started = false, soundOn = true, audio, last = 0, shake = 0, flash = 0;
let mouse = { x: innerWidth * .7, y: innerHeight * .5 };
let cameraY = 0, bestHeight = +(localStorage.getItem("kickback-best") || 0), shotCount = 0;
const worldWidth = 900, topHeight = 5600;
const player = { x: 450, y: 70, vx: 0, vy: 0, r: 18, angle: 0, loaded: true, grounded: false, reloadGlow: 0 };
let particles = [], trails = [], bullet = null;

const platforms = [
  [245,20,410,28,0],[535,245,240,22,-.05],[175,470,215,22,.08],[470,720,175,20,-.12],[700,920,130,19,0],
  [360,1110,125,18,.14],[85,1325,220,22,-.08],[415,1535,305,22,.05],[705,1790,120,18,-.18],[345,1985,155,20,0],
  [80,2180,180,22,.12],[425,2410,125,18,-.08],[660,2630,180,20,.15],[300,2830,250,22,0],[60,3090,150,19,-.12],
  [390,3295,115,18,.1],[685,3510,145,20,0],[450,3740,110,18,-.16],[110,3960,250,22,.08],[510,4180,225,20,-.08],
  [725,4405,110,18,.2],[390,4600,150,19,0],[95,4805,205,22,-.1],[410,5010,105,18,.12],[670,5200,170,22,0],[305,5420,285,28,0]
].map((p,i)=>({x:p[0],y:p[1],w:p[2],h:p[3],a:p[4],moving:i===7||i===15,baseX:p[0],phase:i}));
const walls = [{x:40,y:820,w:22,h:360},{x:838,y:1410,w:22,h:390},{x:40,y:2500,w:22,h:430},{x:838,y:3660,w:22,h:350}];
const sections=[{y:0,n:"01",name:"The Concrete Nursery"},{y:1400,n:"02",name:"Bad Decisions"},{y:2800,n:"03",name:"The Long Regret"},{y:4100,n:"04",name:"Aim Somewhere Else"},{y:5200,n:"05",name:"The Last Laugh"}];

function resize(){W=canvas.width=innerWidth*devicePixelRatio;H=canvas.height=innerHeight*devicePixelRatio;scale=Math.min(innerWidth/900,1.3);ctx.setTransform(devicePixelRatio,0,0,devicePixelRatio,0,0)}
addEventListener("resize",resize);resize();
function tone(freq,duration,type="sine",volume=.08){if(!soundOn)return;audio ||= new AudioContext();const o=audio.createOscillator(),g=audio.createGain();o.type=type;o.frequency.setValueAtTime(freq,audio.currentTime);o.frequency.exponentialRampToValueAtTime(Math.max(35,freq*.4),audio.currentTime+duration);g.gain.setValueAtTime(volume,audio.currentTime);g.gain.exponentialRampToValueAtTime(.001,audio.currentTime+duration);o.connect(g).connect(audio.destination);o.start();o.stop(audio.currentTime+duration)}
function reset(){Object.assign(player,{x:450,y:70,vx:0,vy:0,loaded:true,grounded:false});cameraY=0;shotCount=0;particles=[];bullet=null;ui.finish.hidden=true;updateUI()}
function fire(){if(!started||!player.loaded)return;const sx=(player.x-worldWidth/2)*scale+innerWidth/2,sy=innerHeight*.66-(player.y-cameraY)*scale;const dx=(mouse.x-sx)/scale,dy=-(mouse.y-sy)/scale,len=Math.hypot(dx,dy)||1,nx=dx/len,ny=dy/len;player.vx-=nx*620;player.vy-=ny*620;player.loaded=false;player.grounded=false;bullet={x:player.x+nx*34,y:player.y+ny*34,vx:nx*1150,vy:ny*1150,life:.7};shotCount++;shake=14;flash=1;tone(110,.22,"sawtooth",.15);for(let i=0;i<18;i++)particles.push({x:player.x+nx*35,y:player.y+ny*35,vx:nx*(80+Math.random()*180)+(Math.random()-.5)*150,vy:ny*(80+Math.random()*180)+(Math.random()-.5)*150,life:.2+Math.random()*.25,c:"#e9ff4a"});updateUI()}
canvas.addEventListener("pointermove",e=>{mouse={x:e.clientX,y:e.clientY};ui.crosshair.style.left=e.clientX+"px";ui.crosshair.style.top=e.clientY+"px"});
canvas.addEventListener("pointerdown",fire);
ui.startButton.onclick=()=>{started=true;ui.start.classList.add("hidden");audio ||= new AudioContext();setTimeout(()=>ui.start.remove(),700)};
ui.restart.onclick=reset;
ui.soundButton.onclick=()=>{soundOn=!soundOn;ui.soundButton.classList.toggle("muted",!soundOn);ui.soundLabel.textContent=soundOn?"Sound on":"Sound off"};

function worldToScreen(x,y){return{x:(x-worldWidth/2)*scale+innerWidth/2,y:innerHeight*.66-(y-cameraY)*scale}}
function updateUI(){const a=Math.max(0,Math.floor(player.y/10));bestHeight=Math.max(bestHeight,a);localStorage.setItem("kickback-best",bestHeight);ui.altitude.textContent=a;ui.best.textContent=bestHeight;ui.shots.textContent=shotCount;ui.fill.style.width=Math.min(100,a/(topHeight/10)*100)+"%";ui.bullet.classList.toggle("empty",!player.loaded);ui.bullet.classList.toggle("ready",player.loaded);ui.bulletText.textContent=player.loaded?"Ready":"Empty";const s=[...sections].reverse().find(v=>player.y>=v.y)||sections[0];ui.section.innerHTML=`<span>${s.n}</span> ${s.name}`}
function collideSurface(s,dt){const cx=Math.max(s.x,Math.min(player.x,s.x+s.w)),cy=Math.max(s.y,Math.min(player.y,s.y+s.h));const dx=player.x-cx,dy=player.y-cy;if(dx*dx+dy*dy<player.r*player.r){const prevY=player.y-player.vy*dt;if(prevY>=s.y+s.h-3&&player.vy<0){player.y=s.y+s.h+player.r;player.vy=Math.max(0,-player.vy*.12);player.vx*=.78;player.grounded=true;reload()}else if(Math.abs(dx)>Math.abs(dy)){player.x=cx+(dx<0?-player.r:player.r);player.vx*=-.28;reload()}else{player.y=cy+(dy<0?-player.r:player.r);player.vy*=-.25;reload()}}}
function reload(){if(!player.loaded){player.loaded=true;player.reloadGlow=1;tone(480,.09,"square",.035);updateUI()}}
function update(dt,time){if(!started)return;platforms.forEach(p=>{if(p.moving)p.x=p.baseX+Math.sin(time*.001+p.phase)*95});player.grounded=false;player.vy-=1250*dt;player.vx*=Math.pow(.997,dt*60);player.vy*=Math.pow(.999,dt*60);player.x+=player.vx*dt;player.y+=player.vy*dt;if(player.x-player.r<25){player.x=25+player.r;player.vx=Math.abs(player.vx)*.35;reload()}if(player.x+player.r>875){player.x=875-player.r;player.vx=-Math.abs(player.vx)*.35;reload()}platforms.forEach(p=>collideSurface(p,dt));walls.forEach(w=>collideSurface(w,dt));if(player.y<45){player.y=45;player.vy=Math.max(0,player.vy);reload()}if(player.y>topHeight+120){ui.finish.hidden=false;started=false}const target=Math.max(0,player.y-innerHeight/(scale)*.54);cameraY+=(target-cameraY)*Math.min(1,dt*4.5);player.angle=Math.atan2(-(mouse.y-(innerHeight*.66-(player.y-cameraY)*scale)),mouse.x-((player.x-worldWidth/2)*scale+innerWidth/2));player.reloadGlow=Math.max(0,player.reloadGlow-dt*2);if(bullet){bullet.x+=bullet.vx*dt;bullet.y+=bullet.vy*dt;bullet.life-=dt;trails.push({x:bullet.x,y:bullet.y,life:.14});if(bullet.life<=0)bullet=null}particles.forEach(p=>{p.x+=p.vx*dt;p.y+=p.vy*dt;p.vy-=500*dt;p.life-=dt});particles=particles.filter(p=>p.life>0);trails.forEach(t=>t.life-=dt);trails=trails.filter(t=>t.life>0);shake*=Math.pow(.03,dt);flash=Math.max(0,flash-dt*5);ui.tip.style.opacity=shotCount>1?0:1;updateUI()}

function drawPlatform(p){const q=worldToScreen(p.x,p.y),w=p.w*scale,h=p.h*scale;ctx.save();ctx.translate(q.x,q.y);ctx.rotate(-p.a);ctx.fillStyle="#aeb4ae";ctx.fillRect(0,-h,w,h);ctx.fillStyle="#e9ff4a";ctx.fillRect(0,-h,Math.min(28,w),3);ctx.strokeStyle="#626968";ctx.lineWidth=1;for(let x=16;x<w;x+=28){ctx.beginPath();ctx.moveTo(x,-h);ctx.lineTo(x-12,0);ctx.stroke()}ctx.restore()}
function draw(){ctx.setTransform(devicePixelRatio,0,0,devicePixelRatio,0,0);ctx.clearRect(0,0,innerWidth,innerHeight);const sx=(Math.random()-.5)*shake,sy=(Math.random()-.5)*shake;ctx.save();ctx.translate(sx,sy);const grad=ctx.createLinearGradient(0,0,0,innerHeight);grad.addColorStop(0,"#172024");grad.addColorStop(1,"#0d1113");ctx.fillStyle=grad;ctx.fillRect(-20,-20,innerWidth+40,innerHeight+40);ctx.strokeStyle="#222a2c";ctx.lineWidth=1;for(let x=innerWidth/2%70;x<innerWidth;x+=70){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,innerHeight);ctx.stroke()}for(let y=((cameraY*scale)%70);y<innerHeight;y+=70){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(innerWidth,y);ctx.stroke()}walls.forEach(drawPlatform);platforms.forEach(drawPlatform);trails.forEach(t=>{const p=worldToScreen(t.x,t.y);ctx.fillStyle=`rgba(233,255,74,${t.life*5})`;ctx.fillRect(p.x-5,p.y-1,10,2)});particles.forEach(o=>{const p=worldToScreen(o.x,o.y);ctx.globalAlpha=Math.min(1,o.life*4);ctx.fillStyle=o.c;ctx.fillRect(p.x,p.y,3,3)});ctx.globalAlpha=1;if(bullet){const b=worldToScreen(bullet.x,bullet.y);ctx.fillStyle="#e9ff4a";ctx.beginPath();ctx.arc(b.x,b.y,4,0,7);ctx.fill()}const p=worldToScreen(player.x,player.y);if(player.reloadGlow){ctx.strokeStyle=`rgba(233,255,74,${player.reloadGlow})`;ctx.lineWidth=2;ctx.beginPath();ctx.arc(p.x,p.y,27+10*(1-player.reloadGlow),0,7);ctx.stroke()}ctx.save();ctx.translate(p.x,p.y);ctx.fillStyle="#e4e2db";ctx.beginPath();ctx.arc(0,0,player.r*scale,0,7);ctx.fill();ctx.fillStyle="#171b1c";ctx.fillRect(-9,-3,6,5);ctx.rotate(-player.angle);ctx.fillStyle="#ff5a2f";ctx.fillRect(4,-5,34,10);ctx.fillStyle="#cfcfc7";ctx.fillRect(31,-3,12,6);ctx.restore();ctx.fillStyle="#8b9291";ctx.font="9px DM Mono";ctx.textAlign="center";ctx.fillText(player.loaded?"CHAMBERED":"RELOAD ON CONTACT",p.x,p.y+35);if(flash){ctx.fillStyle=`rgba(233,255,74,${flash*.08})`;ctx.fillRect(0,0,innerWidth,innerHeight)}ctx.restore()}
function loop(t){const dt=Math.min(.025,(t-last)/1000||0);last=t;update(dt,t);draw();requestAnimationFrame(loop)}requestAnimationFrame(loop);
