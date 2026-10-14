export function focus(id) { document.getElementById(id)?.focus(); }
export function initialize() {
    const keydown = e => {
        if (e.target.closest('[role="tablist"]') && ['ArrowRight', 'ArrowLeft', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(e.key)) e.preventDefault();
    };
    document.addEventListener('keydown', keydown);
    return { dispose() { document.removeEventListener('keydown', keydown); } };
}
