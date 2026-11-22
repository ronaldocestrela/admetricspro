(function () {
    'use strict';

    var STORAGE_KEY = 'admetrics-theme';

  /**
   * Resolve o tema efetivo (light/dark) a partir da preferência salva e do sistema.
   * @returns {'light'|'dark'}
   */
    function resolveTheme() {
        var saved = null;
        try {
            saved = window.localStorage.getItem(STORAGE_KEY);
        } catch (e) {
            saved = null;
        }

        if (saved === 'light' || saved === 'dark') {
            return saved;
        }

        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }

        return 'light';
    }

  /**
   * Aplica o atributo data-theme no documento.
   * @param {'light'|'dark'} theme
   */
    function applyTheme(theme) {
        var value = theme === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', value);
        document.documentElement.style.colorScheme = value;
        try {
            window.dispatchEvent(new CustomEvent('admetrics-theme-changed', { detail: { theme: value } }));
        } catch (e) {
            /* noop */
        }
    }

  /**
   * Lê o tema atual do documento.
   * @returns {'light'|'dark'}
   */
    function getCurrentTheme() {
        var attr = document.documentElement.getAttribute('data-theme');
        return attr === 'dark' ? 'dark' : 'light';
    }

  /**
   * Alterna entre claro e escuro, persiste e reaplica.
   * @returns {'light'|'dark'}
   */
    function toggleTheme() {
        var next = getCurrentTheme() === 'dark' ? 'light' : 'dark';
        try {
            window.localStorage.setItem(STORAGE_KEY, next);
        } catch (e) {
            /* noop */
        }
        applyTheme(next);
        return next;
    }

  /**
   * Define um tema explicitamente e persiste.
   * @param {'light'|'dark'} theme
   */
    function setTheme(theme) {
        var value = theme === 'dark' ? 'dark' : 'light';
        try {
            window.localStorage.setItem(STORAGE_KEY, value);
        } catch (e) {
            /* noop */
        }
        applyTheme(value);
    }

    function initFromStorage() {
        applyTheme(resolveTheme());
    }

    window.admetricsTheme = {
        resolve: resolveTheme,
        apply: applyTheme,
        toggle: toggleTheme,
        set: setTheme,
        get: getCurrentTheme,
        storageKey: STORAGE_KEY
    };

    initFromStorage();
})();