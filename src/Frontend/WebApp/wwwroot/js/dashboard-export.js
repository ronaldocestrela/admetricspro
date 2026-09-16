/**
 * AdMetricsPro — Rotinas de Exportação Rápida do Dashboard (CSV & Imagem Vetorial)
 */
window.dashboardExport = {
    /**
     * Efetua o download do arquivo CSV no navegador com suporte a caracteres acentuados (UTF-8 BOM).
     * @param {string} fileName - Nome do arquivo para download.
     * @param {string} content - Conteúdo delimitado em texto.
     */
    downloadCsv: function (fileName, content) {
        var bom = '\uFEFF';
        var blob = new Blob([bom + content], { type: 'text/csv;charset=utf-8;' });
        var link = document.createElement('a');
        var url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', fileName);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    },

    /**
     * Dispara a captura ou impressão executiva vetorial em alta resolução da visão consolidada.
     * @param {string} elementId - Identificador do container HTML do painel.
     */
    exportImageOrPrint: function (elementId) {
        var container = document.getElementById(elementId);
        if (!container) return;

        // Dispara modo de impressão otimizado para PDF/Imagem vetorial de alta definição
        window.print();
    }
};
