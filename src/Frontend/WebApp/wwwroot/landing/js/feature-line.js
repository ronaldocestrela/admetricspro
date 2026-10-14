export function lineFrames(from, to) {
    const css = box => ({ left: `${box.left}px`, top: `${box.top}px`, width: `${box.width}px` });
    const right = to.left + to.width;
    const oldRight = from.left + from.width;
    const stretch = to.left >= from.left
        ? { left: from.left, top: to.top, width: Math.max(oldRight, right) - from.left }
        : { left: to.left, top: to.top, width: Math.max(oldRight, right) - to.left };
    return [{ ...css(from), offset: 0 }, { ...css(stretch), offset: 0.55 }, { ...css(to), offset: 1 }];
}

export function initialize(grid, line, initial) {
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    let selected = initial, animation = null, disposed = false;
    function bounds(element) {
        // Layout coordinates keep the neon aligned while cards translate during scroll reveal.
        return { left: element.offsetLeft, top: element.offsetTop + element.offsetHeight - 3, width: element.offsetWidth };
    }
    function position(box) {
        line.style.left = `${box.left}px`; line.style.top = `${box.top}px`; line.style.width = `${box.width}px`;
    }
    function select(id, animate = true) {
        if (disposed) return;
        const card = grid.querySelector(`[data-feature="${id}"]`);
        if (!card) return;
        const to = bounds(card);
        const lineRect = line.getBoundingClientRect(), gridRect = grid.getBoundingClientRect();
        const from = { left: lineRect.left - gridRect.left, top: lineRect.top - gridRect.top, width: lineRect.width };
        animation?.cancel(); animation = null; selected = id;
        if (!animate || motion.matches || !from.width || typeof line.animate !== 'function') { position(to); return; }
        // First stretch the leading edge to the new topic, then bring the trailing edge across.
        const running = line.animate(lineFrames(from, to), { duration: 620, easing: 'cubic-bezier(0.4,0,0.2,1)', fill: 'forwards' });
        animation = running;
        running.onfinish = () => {
            if (animation !== running || disposed) return;
            position(to); running.cancel(); animation = null;
        };
    }
    const resize = () => select(selected, false);
    const observer = new ResizeObserver(resize); observer.observe(grid);
    motion.addEventListener('change', resize); select(initial, false);
    return { select, dispose() {
        disposed = true; animation?.cancel(); observer.disconnect(); motion.removeEventListener('change', resize);
    } };
}
