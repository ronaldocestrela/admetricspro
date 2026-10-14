export function initialize(dotnet) {
    let active = '';
    const ids = ['hero', 'como-funciona', 'recursos', 'para-quem', 'planos', 'faq'];
    const update = () => {
        let current = 'hero', top = -Infinity;
        for (const id of ids) {
            const section = document.getElementById(id);
            if (!section) continue;
            const position = section.getBoundingClientRect().top;
            if (position <= 180 && position > top) { current = id; top = position; }
        }
        if (current !== active) { active = current; dotnet.invokeMethodAsync('SetActiveSection', current); }
    };
    const observer = new IntersectionObserver(update, { rootMargin: '-76px 0px -60% 0px', threshold: [0, 0.1, 0.5] });
    const observe = () => { for (const id of ids) { const section = document.getElementById(id); if (section) observer.observe(section); } update(); };
    const mutations = new MutationObserver(observe);
    const main = document.getElementById('main-content');
    if (main) mutations.observe(main, { childList: true });
    observe();
    window.addEventListener('hashchange', update);
    return { dispose() { observer.disconnect(); mutations.disconnect(); window.removeEventListener('hashchange', update); } };
}
