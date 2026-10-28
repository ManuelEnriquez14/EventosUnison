// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Barra lateral en pantallas pequeñas
(function () {
    const sidebar = document.getElementById('sidebar');
    const fondo = document.getElementById('sidebarFondo');
    const boton = document.getElementById('btnMenu');
    if (!sidebar) return;

    function alternar(abrir) {
        sidebar.classList.toggle('abierto', abrir);
        fondo.classList.toggle('visible', abrir);
    }
    boton?.addEventListener('click', function () { alternar(true); });
    fondo.addEventListener('click', function () { alternar(false); });
    sidebar.addEventListener('click', function (e) {
        if (e.target.closest('a')) alternar(false);
    });
})();

// Ventanita de notificaciones: se carga al abrir la campana
document.addEventListener('show.bs.dropdown', async function (e) {
    if (e.target.id !== 'btnCampana') return;

    const contenido = document.getElementById('notifContenido');
    try {
        const r = await fetch(e.target.dataset.url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!r.ok) throw new Error();
        contenido.innerHTML = await r.text();

        const pendientes = parseInt(r.headers.get('X-Pendientes') || '0', 10);
        const badge = document.getElementById('campanaBadge');
        badge.textContent = pendientes;
        badge.hidden = pendientes === 0;
    } catch (err) {
        contenido.textContent = 'No se pudieron cargar las notificaciones.';
    }
});

// Contador de caracteres: <textarea data-contador="idDelSpan" data-max="500">
document.querySelectorAll('[data-contador]').forEach(function (campo) {
    const salida = document.getElementById(campo.dataset.contador);
    const max = parseInt(campo.dataset.max, 10);
    const actualizar = function () { salida.textContent = campo.value.length + ' / ' + max; };
    campo.addEventListener('input', actualizar);
    actualizar();
});