export function initialize(dotnet) {
    const desktop = matchMedia('(min-width: 1024px)');
    const close = () => dotnet.invokeMethodAsync('CloseMenu');
    const key = e => {
        const toggle = document.getElementById('mobile-menu-toggle');
        if (e.key === 'Escape' && toggle?.getAttribute('aria-expanded') === 'true') { close(); toggle.focus(); }
    };
    const change = () => { if (desktop.matches) close(); };
    document.addEventListener('keydown', key); desktop.addEventListener('change', change);
    return { dispose() { document.removeEventListener('keydown', key); desktop.removeEventListener('change', change); } };
}
