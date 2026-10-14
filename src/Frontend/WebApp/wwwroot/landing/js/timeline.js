export function initialize(track) {
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    const nodes = [...track.querySelectorAll('.timeline-node')];
    const progress = track.querySelector('.timeline-progress'), cursor = track.querySelector('.timeline-cursor');
    let position = 0, target = 0, frame = 0, disposed = false;
    const handlers = [];
    track.classList.add('is-interactive');
    const center = node => node.offsetTop + node.offsetHeight / 2;
    function reveal(node) {
        if (node.classList.contains('is-revealed')) return;
        node.classList.add('is-revealed');
        node.querySelector('.timeline-pin').setAttribute('aria-expanded', 'true');
    }
    function paint() {
        progress.style.height = `${position}px`; cursor.style.top = `${position}px`;
        for (const node of nodes) if (position >= center(node) - 20) reveal(node);
    }
    function tick() {
        frame = 0;
        if (disposed) return;
        position += (target - position) * 0.16;
        if (Math.abs(target - position) < 0.5) position = target;
        paint();
        if (position !== target) frame = requestAnimationFrame(tick);
    }
    function scroll() {
        if (track.dataset.revealState === 'pending') return;
        // The activation point stays at 55% of the viewport as the page scrolls past it.
        target = Math.max(0, Math.min(track.offsetHeight, innerHeight * 0.55 - track.getBoundingClientRect().top));
        if (motion.matches) {
            cancelAnimationFrame(frame); frame = 0;
            position = target; nodes.forEach(reveal); paint(); return;
        }
        if (!frame) frame = requestAnimationFrame(tick);
    }
    nodes.forEach(node => {
        const pin = node.querySelector('.timeline-pin');
        const activate = () => reveal(node);
        pin.addEventListener('click', activate); handlers.push([pin, activate]);
    });
    const observer = new MutationObserver(scroll);
    observer.observe(track, { attributes: true, attributeFilter: ['data-reveal-state'] });
    window.addEventListener('scroll', scroll, { passive: true }); window.addEventListener('resize', scroll);
    motion.addEventListener('change', scroll); scroll();
    return { dispose() {
        disposed = true; cancelAnimationFrame(frame); observer.disconnect(); track.classList.remove('is-interactive');
        window.removeEventListener('scroll', scroll); window.removeEventListener('resize', scroll);
        motion.removeEventListener('change', scroll);
        for (const [pin, handler] of handlers) pin.removeEventListener('click', handler);
    } };
}
