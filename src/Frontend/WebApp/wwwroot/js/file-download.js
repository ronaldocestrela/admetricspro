/**
 * AdMetricsPro — download de arquivo a partir de conteúdo Base64 no navegador.
 */
window.fileDownload = {
    /**
     * Dispara o download de um arquivo decodificado de Base64.
     * @param {string} fileName - Nome sugerido para o arquivo.
     * @param {string} mimeType - Tipo MIME do blob.
     * @param {string} contentBase64 - Conteúdo em Base64 (sem prefixo data:).
     */
    downloadBase64: function (fileName, mimeType, contentBase64) {
        var binary = atob(contentBase64);
        var length = binary.length;
        var bytes = new Uint8Array(length);
        for (var i = 0; i < length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        var blob = new Blob([bytes], { type: mimeType || 'application/octet-stream' });
        var link = document.createElement('a');
        var url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', fileName);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};