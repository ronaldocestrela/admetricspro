/**
 * AdMetricsPro — suporte mínimo ao drag-and-drop HTML5 no calendário orgânico (Blazor Server).
 * Preenche dataTransfer no dragstart síncrono do navegador; a regra de negócio permanece no backend via RescheduleAsync.
 */
(function () {
    function onDragStart(event) {
        var card = event.target.closest('[data-testid^="post-card-"]');
        if (!card || card.getAttribute('draggable') !== 'true') {
            return;
        }

        var testId = card.getAttribute('data-testid') || '';
        var postId = testId.replace('post-card-', '');
        if (!postId || !event.dataTransfer) {
            return;
        }

        event.dataTransfer.setData('text/plain', postId);
        event.dataTransfer.effectAllowed = 'move';
    }

    function install() {
        if (window.__admetricsSocialCalendarDragInstalled) {
            return;
        }

        window.__admetricsSocialCalendarDragInstalled = true;
        document.addEventListener('dragstart', onDragStart, true);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', install);
    } else {
        install();
    }
})();