export function initialize(element, dotnet) {
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    let timer = 0, hovered = false, focused = false, inView = false, disposed = false;
    function sync() {
        clearInterval(timer); timer = 0;
        if (!disposed && inView && !document.hidden && !motion.matches && !hovered && !focused)
            timer = setInterval(() => dotnet.invokeMethodAsync('AdvanceSlide'), 4000);
    }
    const enter = () => { hovered = true; sync(); };
    const leave = () => { hovered = false; sync(); };
    const focus = () => { focused = true; sync(); };
    const blur = e => { focused = element.contains(e.relatedTarget); sync(); };
    const observer = new IntersectionObserver(entries => { inView = entries[0].isIntersecting; sync(); }, { threshold: 0.1 });
    observer.observe(element);
    element.addEventListener('pointerenter', enter); element.addEventListener('pointerleave', leave);
    element.addEventListener('focusin', focus); element.addEventListener('focusout', blur);
    document.addEventListener('visibilitychange', sync); motion.addEventListener('change', sync);
    return { dispose() {
        disposed = true; clearInterval(timer); observer.disconnect();
        element.removeEventListener('pointerenter', enter); element.removeEventListener('pointerleave', leave);
        element.removeEventListener('focusin', focus); element.removeEventListener('focusout', blur);
        document.removeEventListener('visibilitychange', sync); motion.removeEventListener('change', sync);
    } };
}
