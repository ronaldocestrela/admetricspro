export function initialize(canvas) {
    const ctx = canvas.getContext('2d');
    if (!ctx) return { dispose() {} };
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    const pointer = { x: -2000, y: -2000, present: false };
    let width = 0, height = 0, frame = 0, previousTime = 0, disposed = false, particles = [];

    function resize() {
        const oldWidth = width, oldHeight = height;
        width = canvas.clientWidth; height = canvas.clientHeight;
        const dpr = Math.min(devicePixelRatio || 1, 2);
        canvas.width = Math.round(width * dpr); canvas.height = Math.round(height * dpr);
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        if (particles.length && oldWidth && oldHeight) {
            for (const p of particles) {
                p.baseX *= width / oldWidth; p.x *= width / oldWidth;
                p.baseY *= height / oldHeight; p.y *= height / oldHeight;
            }
        } else {
            const count = Math.min(65, Math.max(20, Math.round(width * height / 18000)));
            particles = Array.from({ length: count }, () => {
                const x = Math.random() * width, y = Math.random() * height;
                return { x, y, baseX: x, baseY: y, radius: (0.8 + Math.random() * 1.2) * 3,
                    color: Math.random() > 0.4 ? 'rgba(67,133,245,0.65)' : 'rgba(12,184,204,0.65)' };
            });
        }
        draw(0);
    }

    function draw(step) {
        ctx.clearRect(0, 0, width, height);
        for (const p of particles) {
            if (step) {
                const distance = Math.hypot(pointer.x - p.x, pointer.y - p.y);
                const proximity = pointer.present ? Math.max(0, 1 - distance / 300) : 0;
                const influence = proximity * proximity * (3 - 2 * proximity);
                const targetX = p.baseX + (pointer.x - p.baseX) * influence * 0.95;
                const targetY = p.baseY + (pointer.y - p.baseY) * influence * 0.95;
                // Stable initial positions; slow, frame-rate-independent return when the pointer moves away.
                const easing = 1 - Math.pow(1 - (0.005 + influence * 0.035), step);
                p.x += (targetX - p.x) * easing;
                p.y += (targetY - p.y) * easing;
            }
        }
        ctx.shadowBlur = 0; ctx.lineWidth = 0.75;
        for (let i = 0; i < particles.length; i++) for (let j = i + 1; j < particles.length; j++) {
            const a = particles[i], b = particles[j];
            const distance = Math.hypot(a.x - b.x, a.y - b.y);
            if (distance >= 150) continue;
            ctx.strokeStyle = `rgba(75,216,236,${(1 - distance / 150) * 0.18})`;
            ctx.beginPath(); ctx.moveTo(a.x, a.y); ctx.lineTo(b.x, b.y); ctx.stroke();
        }
        if (pointer.present && !motion.matches) {
            for (const p of particles) {
                const alpha = connectionStrength(Math.hypot(pointer.x - p.x, pointer.y - p.y));
                if (!alpha) continue;
                ctx.strokeStyle = `rgba(60,170,255,${alpha})`;
                ctx.shadowColor = `rgba(30,140,255,${alpha})`;
                ctx.shadowBlur = 18 * alpha; ctx.lineWidth = 1 + alpha;
                ctx.beginPath(); ctx.moveTo(pointer.x, pointer.y); ctx.lineTo(p.x, p.y); ctx.stroke();
            }
        }
        ctx.shadowBlur = 0;
        for (const p of particles) {
            ctx.fillStyle = p.color;
            ctx.beginPath(); ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2); ctx.fill();
        }
    }

    function tick(time) {
        frame = 0;
        if (disposed || motion.matches || document.hidden) return;
        const step = previousTime && Number.isFinite(time) ? Math.min(3, (time - previousTime) / (1000 / 60)) : 1;
        previousTime = time || 0;
        draw(step); frame = requestAnimationFrame(tick);
    }
    function restart() {
        cancelAnimationFrame(frame); frame = 0; previousTime = 0;
        if (motion.matches) {
            for (const p of particles) { p.x = p.baseX; p.y = p.baseY; }
            draw(0); return;
        }
        if (!document.hidden && !disposed) frame = requestAnimationFrame(tick);
    }
    function move(e) {
        if (e.pointerType === 'touch') return;
        pointer.x = e.clientX; pointer.y = e.clientY; pointer.present = true;
    }
    function leave() { pointer.present = false; }
    const observer = new ResizeObserver(resize);
    observer.observe(canvas); resize(); restart();
    window.addEventListener('pointermove', move, { passive: true });
    document.addEventListener('pointerleave', leave);
    window.addEventListener('blur', leave);
    document.addEventListener('visibilitychange', restart);
    motion.addEventListener('change', restart);
    return { dispose() {
        disposed = true; cancelAnimationFrame(frame); observer.disconnect();
        window.removeEventListener('pointermove', move);
        document.removeEventListener('pointerleave', leave);
        window.removeEventListener('blur', leave);
        document.removeEventListener('visibilitychange', restart);
        motion.removeEventListener('change', restart);
    } };
}

export function connectionStrength(distance) {
    // Full neon brightness within 45px, then fade continuously until the 300px connection limit.
    return Math.pow(Math.max(0, Math.min(1, (300 - distance) / 255)), 1.2);
}

export function initializeParticles(canvasId) {
    const canvas = document.getElementById(canvasId);
    return canvas ? initialize(canvas) : { dispose() {} };
}
