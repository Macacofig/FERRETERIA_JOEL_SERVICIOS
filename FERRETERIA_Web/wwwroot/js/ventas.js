// FERRETERÍA JOEL — Historial de ventas.
// Modal de detalle (solo lectura) y anulación con motivo, ambos
// controlados desde la lista. Los datos del detalle provienen de la
// fachada vía handler JSON.

(function () {
    var fmt = new Intl.NumberFormat('es-BO', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    function dinero(n) {
        return fmt.format(n) + ' Bs';
    }

    function crear(tag, clase, texto) {
        var el = document.createElement(tag);
        if (clase) el.className = clase;
        if (texto !== undefined && texto !== null) el.textContent = texto;
        return el;
    }

    function celdaProducto(nombre, codigo) {
        var td = crear('td');
        td.appendChild(crear('span', 'fj-nombre', nombre));
        td.appendChild(crear('span', 'fj-nombre-sub', codigo));
        return td;
    }

    function abrirModal(modal) {
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('fj-modal-open');
    }

    function cerrarModales() {
        document.querySelectorAll('.fj-modal.is-open').forEach(function (m) {
            m.classList.remove('is-open');
            m.setAttribute('aria-hidden', 'true');
        });
        document.body.classList.remove('fj-modal-open');
    }

    // ---------- Detalle (solo lectura) ----------

    var detalleModal = document.getElementById('ventasDetalleModal');
    var codigoEl = document.getElementById('ventasDetalleCodigo');
    var clienteEl = document.getElementById('ventasDetalleCliente');
    var fechaEl = document.getElementById('ventasDetalleFecha');
    var pagoEl = document.getElementById('ventasDetallePago');
    var lineasEl = document.getElementById('ventasDetalleLineas');
    var totalEl = document.getElementById('ventasDetalleTotal');
    var motivoEl = document.getElementById('ventasDetalleMotivo');

    document.addEventListener('click', function (e) {
        var boton = e.target.closest('[data-ventas-detalle]');
        if (!boton || !detalleModal) return;

        e.preventDefault();

        fetch('?handler=Detalle&idVenta=' + boton.getAttribute('data-id'), {
            headers: { 'Accept': 'application/json' }
        }).then(function (r) {
            if (!r.ok) {
                if (r.status === 401) throw new Error('Inicie sesión para consultar ventas.');
                if (r.status === 403) throw new Error('No tiene permisos para consultar el detalle.');
                throw new Error('No se pudo consultar el detalle (HTTP ' + r.status + ').');
            }
            return r.json();
        }).then(function (v) {
            if (!v || !v.lineas) return;

            if (codigoEl) codigoEl.textContent = v.codigoVenta;
            if (clienteEl) clienteEl.textContent = boton.getAttribute('data-cliente') || '';
            if (fechaEl) fechaEl.textContent = new Date(v.fecha).toLocaleString('es-BO');
            if (pagoEl) pagoEl.textContent = v.estadoPago + ' · ' + v.metodoPago;

            if (lineasEl) {
                lineasEl.replaceChildren();
                v.lineas.forEach(function (l) {
                    var tr = crear('tr');
                    tr.appendChild(celdaProducto(l.nombre, l.codigo));
                    tr.appendChild(crear('td', 'fj-td-cantidad', String(l.cantidad)));
                    tr.appendChild(crear('td', 'fj-td-precio', dinero(l.precio)));
                    tr.appendChild(crear('td', 'fj-td-precio', dinero(l.total)));
                    lineasEl.appendChild(tr);
                });
            }

            if (totalEl) totalEl.textContent = dinero(v.total);

            if (motivoEl) {
                if (v.estadoVenta === 'Anulada' && v.motivoAnulacion) {
                    motivoEl.hidden = false;
                    motivoEl.textContent = 'Motivo de anulación: ' + v.motivoAnulacion;
                } else {
                    motivoEl.hidden = true;
                }
            }

            abrirModal(detalleModal);
        }).catch(function (error) {
            window.alert(error.message || 'Error al consultar el detalle de venta.');
        });
    });

    // ---------- Anulación con motivo ----------

    var anularModal = document.getElementById('ventasAnularModal');
    var anularInputId = document.getElementById('ventasAnularId');
    var anularContexto = document.getElementById('ventasAnularContexto');
    var anularMotivo = document.getElementById('ventasAnularMotivo');
    var anularError = document.getElementById('ventasAnularError');
    var anularForm = document.getElementById('ventasAnularForm');

    document.addEventListener('click', function (e) {
        var boton = e.target.closest('[data-ventas-anular]');
        if (!boton || !anularModal) return;

        e.preventDefault();

        var id = boton.getAttribute('data-id');
        var codigo = boton.getAttribute('data-codigo');

        if (anularInputId) anularInputId.value = id;
        if (anularContexto) {
            anularContexto.textContent =
                '¿Anular la venta ' + (codigo || '') +
                '? El stock de los productos será restaurado.';
        }
        if (anularMotivo) anularMotivo.value = '';
        if (anularError) anularError.hidden = true;

        abrirModal(anularModal);
        if (anularMotivo) anularMotivo.focus();
    });

    if (anularForm) {
        anularForm.addEventListener('submit', function (event) {
            if (!anularMotivo || !anularMotivo.value.trim()) {
                event.preventDefault();
                if (anularError) anularError.hidden = false;
                if (anularMotivo) anularMotivo.focus();
                return;
            }
            cerrarModales();
        });
    }

    // ---------- Cierre de modales ----------

    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('fj-modal-backdrop') ||
            e.target.closest('[data-fj-modal-close]')) {
            cerrarModales();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') cerrarModales();
    });
})();
