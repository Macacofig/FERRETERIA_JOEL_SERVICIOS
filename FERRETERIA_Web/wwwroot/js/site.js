// FERRETERÍA JOEL — Interacciones globales (menú móvil, submenú por dominio, accesibilidad).

document.addEventListener('DOMContentLoaded', function () {
    var burger = document.querySelector('[data-fj-burger]');
    var nav = document.getElementById('fjNav');

    // Menú hamburguesa (pantallas pequeñas)
    if (burger && nav) {
        burger.addEventListener('click', function () {
            var abierto = nav.classList.toggle('fj-nav-open');
            burger.setAttribute('aria-expanded', abierto ? 'true' : 'false');
        });
    }

    // Submenú contextual por dominio (despliegue táctil / móvil)
    document.querySelectorAll('[data-fj-mega-toggle]').forEach(function (toggle) {
        toggle.addEventListener('click', function () {
            var item = toggle.closest('.has-mega');
            if (!item) return;
            var abierto = item.classList.toggle('is-open');
            toggle.setAttribute('aria-expanded', abierto ? 'true' : 'false');
        });
    });

    // Cerrar menú móvil al navegar
    document.querySelectorAll('#fjNav a[href]').forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.innerWidth <= 1024 && nav) {
                nav.classList.remove('fj-nav-open');
                burger && burger.setAttribute('aria-expanded', 'false');
            }
        });
    });

    // Cerrar menú móvil y submenús con Escape
    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        nav && nav.classList.remove('fj-nav-open');
        burger && burger.setAttribute('aria-expanded', 'false');
        document.querySelectorAll('.has-mega.is-open').forEach(function (item) {
            item.classList.remove('is-open');
            var t = item.querySelector('[data-fj-mega-toggle]');
            t && t.setAttribute('aria-expanded', 'false');
        });
    });

    // Cerrar submenús al hacer clic fuera
    document.addEventListener('click', function (event) {
        var megaAbiertos = document.querySelectorAll('.has-mega.is-open');
        if (!megaAbiertos.length) return;
        if (!event.target.closest('.has-mega')) {
            megaAbiertos.forEach(function (item) {
                item.classList.remove('is-open');
                var t = item.querySelector('[data-fj-mega-toggle]');
                t && t.setAttribute('aria-expanded', 'false');
            });
        }
    });
});

// ---------------------------------------------------------
// Modal de confirmación reutilizable.
// Cualquier <form> con data-fj-confirm="mensaje" intercepta su
// envío, muestra el modal y solo envía si el usuario confirma.
// ---------------------------------------------------------
(function () {
    var modal = document.getElementById('fjConfirmModal');
    if (!modal) return;

    var mensajeEl = document.getElementById('fjConfirmMessage');
    var botonOk = document.getElementById('fjConfirmOk');
    var formularioPendiente = null;

    function abrirModal(msj, form) {
        formularioPendiente = form;
        if (mensajeEl) mensajeEl.textContent = msj;
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('fj-modal-open');
        if (botonOk) botonOk.focus();
    }

    function cerrarModal() {
        formularioPendiente = null;
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('fj-modal-open');
    }

    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!form || form.nodeName !== 'FORM') return;

        var msj = form.getAttribute('data-fj-confirm');
        if (!msj) return;

        event.preventDefault();
        event.stopImmediatePropagation();
        abrirModal(msj, form);
    });

    if (botonOk) {
        botonOk.addEventListener('click', function () {
            var form = formularioPendiente;
            cerrarModal();
            if (!form) return;
            form.removeAttribute('data-fj-confirm');
            form.submit();
        });
    }

    modal.addEventListener('click', function (event) {
        if (event.target.classList.contains('fj-modal-backdrop') ||
            event.target.closest('[data-fj-modal-close]')) {
            cerrarModal();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') cerrarModal();
    });
})();

// ---------------------------------------------------------
// Normalización de textos de formularios: elimina espacios
// redundantes al inicio/fin antes de enviar.
// ---------------------------------------------------------
(function () {
    document.querySelectorAll('form.fj-form').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('input[type="text"], input[type="tel"], input[type="number"], input:not([type]), textarea').forEach(function (campo) {
                if (campo.readOnly) return;
                if (campo.value) {
                    campo.value = campo.value.trim().replace(/\s+/g, ' ');
                    if (campo.type === 'number') {
                        campo.value = campo.value.replace(/,/g, '.');
                    }
                }
            });
        });
    });
})();

// ---------------------------------------------------------
// Validación en el cliente de los campos con data-fj-regex
// (nombres sin números ni caracteres especiales y teléfonos).
// Los formularios usan novalidate, así que esta rutina
// complementa la validación que siempre ocurre en el servidor.
// ---------------------------------------------------------
(function () {
    function limpiarError(campo) {
        var campoError = campo.closest('.fj-form-field');
        if (!campoError) return;
        campoError.querySelectorAll('[data-fj-error-cliente]').forEach(function (p) {
            p.remove();
        });
        campoError.classList.remove('fj-form-field-error');
    }

    function mostrarError(campo, mensaje) {
        var campoError = campo.closest('.fj-form-field');
        if (!campoError) return;
        if (campoError.querySelector('.fj-form-error')) return;

        campoError.classList.add('fj-form-field-error');

        var p = document.createElement('p');
        p.className = 'fj-form-error';
        p.setAttribute('role', 'alert');
        p.setAttribute('data-fj-error-cliente', 'true');
        p.textContent = mensaje;
        campoError.appendChild(p);
    }

    document.querySelectorAll('form.fj-form').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            var primerInvalido = null;

            form.querySelectorAll('[data-fj-regex]').forEach(function (campo) {
                limpiarError(campo);

                var valor = (campo.value || '').trim();
                if (!valor) return;

                var regex;
                try {
                    regex = new RegExp(campo.getAttribute('data-fj-regex'), 'u');
                } catch (e) {
                    return;
                }

                if (regex.test(valor)) return;

                mostrarError(
                    campo,
                    campo.getAttribute('data-fj-msj') || 'El valor no es válido.');

                if (!primerInvalido) primerInvalido = campo;
            });

            if (primerInvalido) {
                event.preventDefault();
                event.stopImmediatePropagation();
                primerInvalido.focus();
                primerInvalido.scrollIntoView({ block: 'center', behavior: 'smooth' });
            }
        });
    });
})();

// ---------------------------------------------------------
// Modal de detalle (descripción + unidad de medida).
// Se abre desde cualquier elemento con data-fj-detalle y
// toma los datos de sus atributos data-*.
// ---------------------------------------------------------
(function () {
    var modal = document.getElementById('fjDetalleModal');
    if (!modal) return;

    var titulo = document.getElementById('fjDetalleTitle');
    var producto = document.getElementById('fjDetalleProducto');
    var descripcion = document.getElementById('fjDetalleDescripcion');
    var unidad = document.getElementById('fjDetalleUnidad');
    var botonCerrar = modal.querySelector('[data-fj-modal-close].fj-btn');

    function abrir(boton) {
        if (titulo) {
            titulo.textContent = boton.getAttribute('data-titulo') || 'Detalle del producto';
        }
        if (producto) {
            producto.textContent = boton.getAttribute('data-nombre') || '';
        }
        if (descripcion) {
            descripcion.textContent = boton.getAttribute('data-descripcion') || 'Sin descripción registrada.';
        }
        if (unidad) {
            unidad.textContent = boton.getAttribute('data-unidad') || '—';
        }

        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('fj-modal-open');
        if (botonCerrar) botonCerrar.focus();
    }

    function cerrar() {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('fj-modal-open');
    }

    document.addEventListener('click', function (event) {
        var boton = event.target.closest('[data-fj-detalle]');
        if (boton) {
            event.preventDefault();
            abrir(boton);
            return;
        }

        if (event.target.classList.contains('fj-modal-backdrop') ||
            event.target.closest('[data-fj-modal-close]')) {
            cerrar();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') cerrar();
    });
})();

// ---------------------------------------------------------
// Mostrar / ocultar contraseña.
// Cualquier input envuelto en .fj-password con un botón
// [data-fj-toggle-password] alterna entre tipo password y text.
// ---------------------------------------------------------
(function () {
    document.addEventListener('click', function (event) {
        var boton = event.target.closest('[data-fj-toggle-password]');
        if (!boton) return;

        event.preventDefault();

        var contenedor = boton.closest('.fj-password');
        if (!contenedor) return;

        var campo = contenedor.querySelector('input');
        if (!campo) return;

        var estabaVisible = campo.type === 'text';
        campo.type = estabaVisible ? 'password' : 'text';

        contenedor.classList.toggle('is-visible', !estabaVisible);
        boton.setAttribute('aria-pressed', (!estabaVisible).toString());
        boton.setAttribute(
            'aria-label',
            estabaVisible ? 'Mostrar contraseña' : 'Ocultar contraseña');

        campo.focus({ preventScroll: true });
    });
})();