// Reveal titles earlier in the viewport, then the content attached to each title.
export function initializeScrollReveal(root = document.querySelector('[data-scroll-reveal-root]')) {
    if (!root) return { dispose() {} };
    const motion = matchMedia('(prefers-reduced-motion: reduce)');
    const targets = [], timers = new Map();
    let titleObserver, contentObserver, disposed = false;
    const scopes = [...root.querySelectorAll('section'), ...document.querySelectorAll('footer')];
    for (const scope of scopes) {
        let title = scope.querySelector('h1,h2');
        function collect(element, order = 0) {
            if (element.matches('canvas,script,style,[hidden],.hidden:not(.timeline-pin)')) return;
            if (element.matches('[data-reveal-atomic]')) { add(element, 'content', title, 0); return; }
            if (element.matches('h1,h2')) {
                title = element; add(element, 'title', null, 0); return;
            }
            const children = [...element.children];
            const hasTitle = element.querySelector('h1,h2');
            const layout = element.matches('section,footer,.grid') || element.parentElement === scope ||
                (element.classList.contains('flex-col') && !element.querySelector('h3') &&
                 ![...element.classList].some(name => name.startsWith('bg-')));
            if (children.length && (hasTitle || layout)) {
                children.forEach((child, index) => collect(child, index)); return;
            }
            add(element, 'content', title, Math.min(order, 3) * 55);
        }
        function add(element, kind, dependency, stagger) {
            element.dataset.scrollReveal = kind;
            element.dataset.revealState = 'pending';
            targets.push({ element, kind, dependency, stagger, inView: false, shown: false, shownAt: 0 });
        }
        [...scope.children].forEach((element, index) => collect(element, index));
    }
    const records = new Map(targets.map(record => [record.element, record]));
    function show(record) {
        if (record.shown || disposed) return;
        record.shown = true; record.shownAt = performance.now();
        record.element.dataset.revealState = 'visible';
        timers.delete(record);
        (record.kind === 'title' ? titleObserver : contentObserver)?.unobserve(record.element);
        if (record.kind === 'title')
            for (const dependent of targets)
                if (dependent.dependency === record.element && dependent.inView) schedule(dependent);
    }
    function schedule(record) {
        if (record.shown || timers.has(record) || !record.inView) return;
        const title = records.get(record.dependency);
        if (title && !title.shown) return;
        const delay = Math.max(0, title ? 240 - (performance.now() - title.shownAt) : 0) + record.stagger;
        timers.set(record, setTimeout(() => { timers.delete(record); if (record.inView) show(record); }, delay));
    }
    function observeContent(entries) {
        for (const entry of entries) {
            const record = records.get(entry.target);
            if (!record || record.shown) continue;
            record.inView = entry.isIntersecting;
            if (record.inView) schedule(record);
            else if (timers.has(record)) { clearTimeout(timers.get(record)); timers.delete(record); }
        }
    }
    function showAll() {
        titleObserver?.disconnect(); contentObserver?.disconnect();
        for (const timer of timers.values()) clearTimeout(timer);
        timers.clear();
        for (const record of targets) {
            record.shown = true; record.element.dataset.revealState = 'visible';
        }
        root.removeAttribute('data-reveal-pending');
    }
    if (motion.matches || typeof IntersectionObserver === 'undefined') showAll();
    else {
        titleObserver = new IntersectionObserver(entries => {
            for (const entry of entries) {
                const record = records.get(entry.target);
                if (entry.isIntersecting && record) show(record);
            }
        }, { rootMargin: '0px 0px -8% 0px', threshold: 0 });
        contentObserver = new IntersectionObserver(observeContent, { rootMargin: '0px 0px -18% 0px', threshold: 0 });
        for (const record of targets)
            (record.kind === 'title' ? titleObserver : contentObserver).observe(record.element);
        root.removeAttribute('data-reveal-pending');
    }
    const onMotionChange = () => { if (motion.matches) showAll(); };
    motion.addEventListener('change', onMotionChange);
    return { dispose() {
        showAll(); disposed = true;
        motion.removeEventListener('change', onMotionChange);
        for (const record of targets) {
            delete record.element.dataset.scrollReveal; delete record.element.dataset.revealState;
        }
    } };
}
