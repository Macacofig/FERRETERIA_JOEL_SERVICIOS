// FERRETERÍA JOEL — Pantalla de registro de venta.
// Gestiona los dos buscadores, la tabla temporal de líneas (M:N) y
// la previsualización de totales. La validación y el cálculo
// definitivos siempre ocurren en el servidor (FachadaVenta).
//
// Todo el contenido dinámico se construye con nodos del DOM y
// textContent (nunca innerHTML con datos del usuario) para evitar
// inyección de HTML/XSS.

(function () {
    var app = document.getElementById('ventaApp');
    if (!app) return;

    var form = document.getElementById('ventaForm');
    var lineasBody = document.getElementById('lineasBody');
    var lineasVacio = document.getElementById('lineasVacio');
    var lineasInputs = document.getElementById('lineasInputs');

    var urlClientes = app.getAttribute('data-url-clientes');
    var urlProductos = app.getAttribute('data-url-productos');
    var urlRegistrarAjax = app.getAttribute('data-url-registrar-ajax');
    var urlVentas = app.getAttribute('data-url-ventas');

    var fmt = new Intl.NumberFormat('es-BO', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    var lineas = [];

    function dinero(n) {
        return fmt.format(n) + ' Bs';
    }

    function redondear(n) {
        return Math.round((n + Number.EPSILON) * 100) / 100;
    }

    function crear(tag, clase, texto) {
        var el = document.createElement(tag);
        if (clase) el.className = clase;
        if (texto !== undefined && texto !== null) el.textContent = texto;
        return el;
    }

    // ---------- Buscadores ----------

    function errorHttp(codigo) {
        if (codigo === 401) return 'La sesión ha expirado o no se ha iniciado. Inicie sesión para continuar.';
        if (codigo === 403) return 'No tiene permisos para esta operación.';
        if (codigo === 400) return 'La solicitud no es válida. Revise los datos enviados.';
        if (codigo === 404) return 'No se encontró el servicio solicitado. Revise las rutas de Ventas.';
        if (codigo >= 500) return 'Error del servidor o de la conexión con MySQL.';
        return 'No se pudo completar la solicitud (HTTP ' + codigo + ').';
    }

    function leerJson(response) {
        var esJson = (response.headers.get('content-type') || '').includes('application/json');
        return (esJson ? response.json() : Promise.resolve(null))
            .catch(function () { return null; })
            .then(function (datos) {
                if (!response.ok) {
                    var error = new Error(datos && datos.mensaje || errorHttp(response.status));
                    error.status = response.status;
                    error.datos = datos;
                    throw error;
                }
                if (datos === null) {
                    throw new Error('El servidor no devolvió JSON válido. Revise la sesión y la ruta.');
                }
                return datos;
            });
    }

    function buscar(url, termino) {
        var sep = url.indexOf('?') === -1 ? '?' : '&';
        return fetch(url + sep + 'termino=' + encodeURIComponent(termino), {
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        }).then(leerJson);
    }

    function mostrarErrorResultados(contenedor, error) {
        contenedor.replaceChildren(
            crear('p', 'fj-resultado-vacio', error.message || 'Falló la consulta al servidor.'));
        contenedor.hidden = false;
    }

    function conRetardo(fn, ms) {
        var t = null;
        return function () {
            var args = arguments;
            var self = this;
            clearTimeout(t);
            t = setTimeout(function () {
                fn.apply(self, args);
            }, ms);
        };
    }

    function ocultar(el) {
        el.hidden = true;
        el.replaceChildren();
    }

    // ---------- Cliente ----------

    var inputCliente = document.getElementById('buscarCliente');
    var resultadosCliente = document.getElementById('resultadosCliente');
    var clienteId = document.getElementById('clienteId');
    var clienteNombre = document.getElementById('clienteNombre');
    var clienteSeleccionado = document.getElementById('clienteSeleccionado');

    function mostrarCliente(cliente) {
        clienteId.value = cliente.id;
        clienteNombre.value = cliente.nombre;

        clienteSeleccionado.replaceChildren();
        var ico = crear('span', 'fj-seleccion-ico', '✓');
        ico.setAttribute('aria-hidden', 'true');
        clienteSeleccionado.appendChild(ico);
        clienteSeleccionado.appendChild(document.createTextNode(' '));
        clienteSeleccionado.appendChild(crear('strong', null, cliente.nombre));
        clienteSeleccionado.hidden = false;
        secuenciaClientes += 1;
        ocultar(resultadosCliente);
        actualizarBotonRegistrar();
    }

    var secuenciaClientes = 0;
    var buscarClienteRetardado = conRetardo(function () {
        var solicitud = secuenciaClientes;
        var termino = inputCliente.value.trim();
        if (termino.length < 2) {
            ocultar(resultadosCliente);
            return;
        }

        buscar(urlClientes, termino).then(function (clientes) {
            if (solicitud !== secuenciaClientes) return;
            resultadosCliente.replaceChildren();

            if (!clientes.length) {
                resultadosCliente.appendChild(
                    crear('p', 'fj-resultado-vacio', 'Sin clientes para "' + termino + '".'));
                resultadosCliente.hidden = false;
                return;
            }

            clientes.forEach(function (c) {
                var boton = crear('button', 'fj-resultado');
                boton.type = 'button';
                boton.dataset.clienteId = c.id;
                boton.dataset.clienteNombre = c.nombre;
                boton.dataset.clienteCi = c.ci || '';
                boton.appendChild(crear('span', 'fj-resultado-titulo', c.nombre));
                boton.appendChild(
                    crear('span', 'fj-resultado-sub', c.ci ? 'CI ' + c.ci : 'Sin CI'));
                resultadosCliente.appendChild(boton);
            });

            resultadosCliente.hidden = false;
        }).catch(function (error) {
            if (solicitud !== secuenciaClientes) return;
            mostrarErrorResultados(resultadosCliente, error);
        });
    }, 250);

    inputCliente.addEventListener('input', function () {
        secuenciaClientes += 1;
        buscarClienteRetardado();
    });

    resultadosCliente.addEventListener('click', function (e) {
        var boton = e.target.closest('[data-cliente-id]');
        if (!boton) return;
        mostrarCliente({
            id: boton.dataset.clienteId,
            nombre: boton.dataset.clienteNombre,
            ci: boton.dataset.clienteCi
        });
    });

    // ---------- Producto ----------

    var inputProducto = document.getElementById('buscarProducto');
    var resultadosProducto = document.getElementById('resultadosProducto');

    var secuenciaProductos = 0;
    var buscarProductoRetardado = conRetardo(function () {
        var solicitud = secuenciaProductos;
        var termino = inputProducto.value.trim();
        if (!termino) {
            ocultar(resultadosProducto);
            return;
        }

        buscar(urlProductos, termino).then(function (productos) {
            if (solicitud !== secuenciaProductos) return;
            resultadosProducto.replaceChildren();

            if (!productos.length) {
                resultadosProducto.appendChild(
                    crear('p', 'fj-resultado-vacio', 'Sin productos disponibles.'));
                resultadosProducto.hidden = false;
                return;
            }

            productos.forEach(function (p) {
                var agotado = p.stock <= 0;
                var boton = crear('button', 'fj-resultado');
                boton.type = 'button';
                if (agotado) boton.disabled = true;
                boton.dataset.productoId = p.id;
                boton.dataset.productoCodigo = p.codigo;
                boton.dataset.productoNombre = p.nombre;
                boton.dataset.productoUnidad = p.unidad || '';
                boton.dataset.productoPrecio = p.precio;
                boton.dataset.productoStock = p.stock;
                boton.appendChild(crear('span', 'fj-resultado-titulo', p.nombre));
                boton.appendChild(crear('span', 'fj-resultado-sub',
                    p.codigo + ' · ' + dinero(p.precio) +
                    ' · Stock: ' + p.stock + (agotado ? ' · Agotado' : '')));
                resultadosProducto.appendChild(boton);
            });

            resultadosProducto.hidden = false;
        }).catch(function (error) {
            if (solicitud !== secuenciaProductos) return;
            mostrarErrorResultados(resultadosProducto, error);
        });
    }, 250);

    inputProducto.addEventListener('input', function () {
        secuenciaProductos += 1;
        buscarProductoRetardado();
    });

    resultadosProducto.addEventListener('click', function (e) {
        var boton = e.target.closest('[data-producto-id]');
        if (!boton || boton.disabled) return;

        agregarProducto({
            id: parseInt(boton.dataset.productoId, 10),
            codigo: boton.dataset.productoCodigo,
            nombre: boton.dataset.productoNombre,
            unidad: boton.dataset.productoUnidad,
            precio: parseFloat(boton.dataset.productoPrecio),
            stock: parseInt(boton.dataset.productoStock, 10)
        });

        inputProducto.value = '';
        secuenciaProductos += 1;
        inputProducto.focus();
        ocultar(resultadosProducto);
    });

    // ---------- Líneas ----------

    function buscarLinea(idProducto) {
        return lineas.filter(function (l) {
            return l.id === idProducto;
        })[0];
    }

    function agregarProducto(p) {
        var existente = buscarLinea(p.id);

        if (existente) {
            if (existente.cantidad + 1 > p.stock) {
                alerta('Solo hay ' + p.stock + ' unidades disponibles de "' + p.nombre + '".', 'Límite de stock');
                return;
            }
            existente.cantidad += 1;
        } else {
            lineas.push({
                id: p.id,
                codigo: p.codigo,
                nombre: p.nombre,
                unidad: p.unidad,
                precio: p.precio,
                stock: p.stock,
                cantidad: 1
            });
        }

        render();
    }

    function quitarLinea(idProducto) {
        lineas = lineas.filter(function (l) {
            return l.id !== idProducto;
        });
        render();
    }

    function cambiarCantidad(idProducto, cantidad) {
        var linea = buscarLinea(idProducto);
        if (!linea) return;

        if (isNaN(cantidad) || cantidad < 1) {
            cantidad = 1;
        }
        if (cantidad > linea.stock) {
            cantidad = linea.stock;
            alerta('Solo hay ' + linea.stock + ' unidades disponibles de "' + linea.nombre + '".', 'Límite de stock');
        }

        linea.cantidad = Math.floor(cantidad);
        render();
    }

    function alerta(mensaje, tituloAviso) {
        var overlay = crear('div');
        overlay.id = 'fjModalAlertaDark';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.style.cssText = 'position:fixed;top:0;left:0;width:100vw;height:100vh;background-color:rgba(0,0,0,0.75);backdrop-filter:blur(3px);z-index:9999;display:flex;align-items:center;justify-content:center;padding:16px;box-sizing:border-box;';

        var tarjeta = crear('div');
        tarjeta.style.cssText = 'background-color:#1c2026;border:1px solid #2d333b;border-radius:14px;max-width:400px;width:100%;padding:28px 24px;text-align:center;box-shadow:0 20px 45px rgba(0,0,0,0.7);box-sizing:border-box;';

        var iconoBox = crear('div');
        iconoBox.style.cssText = 'width:50px;height:50px;border-radius:12px;background-color:rgba(239,68,68,0.12);border:1.5px solid #FFC824;color:#FFC824;display:flex;align-items:center;justify-content:center;margin:0 auto 16px auto;';

        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '2');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');
        svg.style.cssText = 'width:24px;height:24px;';

        var pathTriangulo = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        pathTriangulo.setAttribute('d', 'M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z');
        var linea1 = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        linea1.setAttribute('x1', '12'); linea1.setAttribute('y1', '9'); linea1.setAttribute('x2', '12'); linea1.setAttribute('y2', '13');
        var linea2 = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        linea2.setAttribute('x1', '12'); linea2.setAttribute('y1', '17'); linea2.setAttribute('x2', '12.01'); linea2.setAttribute('y2', '17');

        svg.appendChild(pathTriangulo);
        svg.appendChild(linea1);
        svg.appendChild(linea2);
        iconoBox.appendChild(svg);

        var titulo = crear('h3', null, tituloAviso || 'Límite de stock');
        titulo.style.cssText = 'color:#ffffff;font-size:1.18rem;font-weight:700;margin:0 0 8px 0;letter-spacing:-0.01em;';

        var desc = crear('p', null, mensaje);
        desc.style.cssText = 'color:#9ca3af;font-size:0.94rem;line-height:1.5;margin:0 0 24px 0;word-break:break-word;';

        var btnCerrar = crear('button', null, 'Entendido');
        btnCerrar.type = 'button';
        btnCerrar.style.cssText = 'background-color:#2a303a;color:#ffffff;border:1px solid #3b4250;border-radius:8px;padding:9px 24px;font-size:0.92rem;font-weight:600;cursor:pointer;transition:background-color 0.15s ease;outline:none;';

        btnCerrar.onmouseenter = function () { btnCerrar.style.backgroundColor = '#38414e'; };
        btnCerrar.onmouseleave = function () { btnCerrar.style.backgroundColor = '#2a303a'; };

        function cerrarModal() {
            overlay.remove();
            document.removeEventListener('keydown', teclaEscape);
        }

        btnCerrar.addEventListener('click', cerrarModal);

        function teclaEscape(e) {
            if (e.key === 'Escape') cerrarModal();
        }
        document.addEventListener('keydown', teclaEscape);

        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) cerrarModal();
        });

        tarjeta.appendChild(iconoBox);
        tarjeta.appendChild(titulo);
        tarjeta.appendChild(desc);
        tarjeta.appendChild(btnCerrar);
        overlay.appendChild(tarjeta);

        document.body.appendChild(overlay);
        btnCerrar.focus();
    }

    function celdaPrecio(valor) {
        var td = crear('td', 'fj-td-precio');
        td.appendChild(crear('span', 'fj-precio', fmt.format(valor)));
        td.appendChild(document.createTextNode(' '));
        td.appendChild(crear('span', 'fj-currency', 'Bs'));
        return td;
    }

    function svgQuitar() {
        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('fill', 'none');
        svg.setAttribute('stroke', 'currentColor');
        svg.setAttribute('stroke-width', '1.9');
        svg.setAttribute('stroke-linecap', 'round');
        svg.setAttribute('stroke-linejoin', 'round');
        svg.setAttribute('aria-hidden', 'true');
        var p1 = document.createElementNS('http://www.w3.org/2000/svg', 'polyline');
        p1.setAttribute('points', '3 6 5 6 21 6');
        var p2 = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        p2.setAttribute('d', 'M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2');
        svg.appendChild(p1);
        svg.appendChild(p2);
        return svg;
    }

    function render() {
        lineasVacio.hidden = lineas.length > 0;

        lineasBody.replaceChildren();
        lineas.forEach(function (l) {
            var tr = crear('tr');
            tr.dataset.linea = l.id;

            var tdProd = crear('td');
            tdProd.appendChild(crear('span', 'fj-nombre', l.nombre));
            tdProd.appendChild(crear('span', 'fj-nombre-sub', l.codigo));

            var tdUnidad = crear('td', 'fj-td-secundario', l.unidad);

            var tdPrecio = celdaPrecio(l.precio);

            var tdCantidad = crear('td', 'fj-td-cantidad');
            var input = crear('input', 'fj-cantidad-input');
            input.type = 'number';
            input.min = '1';
            input.max = String(l.stock);
            input.value = String(l.cantidad);
            input.dataset.cantidad = l.id;
            tdCantidad.appendChild(input);

            var tdImporte = celdaPrecio(redondear(l.precio * l.cantidad));

            var tdAcciones = crear('td', 'fj-td-acciones');
            var btnQuitar = crear('button', 'fj-btn fj-btn-danger fj-btn-icon');
            btnQuitar.type = 'button';
            btnQuitar.title = 'Quitar';
            btnQuitar.setAttribute('aria-label', 'Quitar producto');
            btnQuitar.dataset.quitar = l.id;
            btnQuitar.appendChild(svgQuitar());
            tdAcciones.appendChild(btnQuitar);

            tr.appendChild(tdProd);
            tr.appendChild(tdUnidad);
            tr.appendChild(tdPrecio);
            tr.appendChild(tdCantidad);
            tr.appendChild(tdImporte);
            tr.appendChild(tdAcciones);
            lineasBody.appendChild(tr);
        });

        lineasInputs.replaceChildren();
        lineas.forEach(function (l, i) {
            var idInput = crear('input');
            idInput.type = 'hidden';
            idInput.name = 'Lineas[' + i + '].IdProducto';
            idInput.value = l.id;
            lineasInputs.appendChild(idInput);

            var cantInput = crear('input');
            cantInput.type = 'hidden';
            cantInput.name = 'Lineas[' + i + '].Cantidad';
            cantInput.value = l.cantidad;
            lineasInputs.appendChild(cantInput);
        });

        recalcularTotales();
        actualizarBotonRegistrar();
    }

    lineasBody.addEventListener('input', function (e) {
        var input = e.target.closest('[data-cantidad]');
        if (!input) return;
        cambiarCantidad(parseInt(input.getAttribute('data-cantidad'), 10), parseInt(input.value, 10));
    });

    lineasBody.addEventListener('click', function (e) {
        var boton = e.target.closest('[data-quitar]');
        if (!boton) return;
        quitarLinea(parseInt(boton.getAttribute('data-quitar'), 10));
    });

    // ---------- Totales ----------

    function recalcularTotales() {
        var total = lineas.reduce(function (suma, l) {
            return suma + redondear(l.precio * l.cantidad);
        }, 0);
        total = redondear(total);

        // Total principal
        var elTotal = document.getElementById('totalFinal');
        if (elTotal) elTotal.textContent = dinero(total);

        return { total: total };
    }

    // ---------- Botón Registrar ----------

    var botonRegistrar = document.getElementById('btnRegistrar');
    var botonRegistrarTexto = document.getElementById('btnRegistrarTexto');

    function actualizarBotonRegistrar() {
        if (!botonRegistrar) return;
        var listo = lineas.length > 0 && parseInt(clienteId.value, 10) > 0;
        botonRegistrar.disabled = !listo;
    }

    function marcandoEnvio(activo) {
        if (!botonRegistrar) return;
        botonRegistrar.disabled = activo;
        if (botonRegistrarTexto) {
            botonRegistrarTexto.textContent = activo ? 'Registrando…' : 'Registrar venta';
        }
    }

    // ---------- Registro de la venta (AJAX) ----------

    function registrarVenta(pestanaPdf) {
        if (!urlRegistrarAjax) {
            if (pestanaPdf) pestanaPdf.close();
            alerta('Falta la dirección de registro en la página (data-url-registrar-ajax).',
                'No se pudo registrar');
            return;
        }

        // La redirección al registrar tiene dos destinos:
        //   · una pestaña nueva con el comprobante de la venta;
        //   · la aplicación (esta misma ventana) al historial de ventas.
        // La pestaña se abre de forma síncrona, dentro del gesto del
        // usuario, para que el bloqueador de ventanas emergentes del
        // navegador no la impida; su contenido se fija cuando el servidor
        // responde con la URL del comprobante.
        marcandoEnvio(true);

        fetch(urlRegistrarAjax, {
            method: 'POST',
            body: new FormData(form),
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        }).then(leerJson).then(function (datos) {
            // El comprobante en PDF se abre en la pestaña que ya se reservó
            // al confirmar (evita que el bloqueador de ventanas emergentes la
            // anule) y la pestaña actual va al historial.
            if (datos && datos.urlPdf && pestanaPdf && !pestanaPdf.closed) {
                pestanaPdf.location.href = datos.urlPdf;
            } else if (datos && datos.urlPdf) {
                window.open(datos.urlPdf, '_blank');
            } else if (pestanaPdf && !pestanaPdf.closed) {
                pestanaPdf.close();
            }
            window.location.href = urlVentas || '/Ventas';
        }).catch(function (error) {
            if (pestanaPdf && !pestanaPdf.closed) {
                pestanaPdf.close();
            }
            alerta(error.message || 'No se pudo registrar la venta.', 'No se pudo registrar');
        }).finally(function () {
            marcandoEnvio(false);
            actualizarBotonRegistrar();
        });
    }

    // ---------- Confirmación ----------

    var modal = document.getElementById('ventaConfirmModal');
    var mensajeModal = document.getElementById('ventaConfirmMessage');
    var botonOk = document.getElementById('ventaConfirmOk');

    function abrirModal(texto) {
        if (!modal) {
            if (window.confirm(texto)) {
                // La pestaña del PDF se reserva en el mismo gesto del clic
                // para que el bloqueador de ventanas emergentes no la anule.
                registrarVenta(window.open('about:blank', '_blank'));
            }
            return;
        }
        mensajeModal.textContent = texto;
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.classList.add('fj-modal-open');
        if (botonOk) botonOk.focus();
    }

    function cerrarModal() {
        if (!modal) return;
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('fj-modal-open');
    }

    botonRegistrar.addEventListener('click', function () {
        if (botonRegistrar.disabled) return;

        if (!lineas.length) {
            alerta('Agregue al menos un producto antes de registrar la venta.', 'Atención');
            return;
        }

        if (parseInt(clienteId.value, 10) <= 0) {
            alerta('Debe seleccionar un cliente.', 'Atención');
            inputCliente.focus();
            return;
        }

        var resumen = recalcularTotales();
        abrirModal(
            'Cliente: ' + clienteNombre.value + ' · ' + lineas.length +
            ' producto(s). Total: ' + dinero(resumen.total) + '. ¿Registrar la venta?');
    });

    if (botonOk) {
        botonOk.addEventListener('click', function () {
            cerrarModal();
            // Se abre la pestaña del PDF dentro del gesto del clic; luego se
            // apunta al comprobante cuando el servidor responde.
            registrarVenta(window.open('about:blank', '_blank'));
        });
    }

    if (modal) {
        modal.addEventListener('click', function (e) {
            if (e.target.classList.contains('fj-modal-backdrop') ||
                e.target.closest('[data-fj-modal-close]')) {
                cerrarModal();
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                cerrarModal();
                cerrarNuevoCliente();
            }
        });
    }

    // ---------- Nuevo cliente (alta sin perder la venta) ----------

    var modalCliente = document.getElementById('nuevoClienteModal');
    var btnNuevoCliente = document.getElementById('btnNuevoCliente');
    var formCliente = document.getElementById('nuevoClienteForm');
    var errorCliente = document.getElementById('nuevoClienteError');

    function abrirNuevoCliente() {
        if (!modalCliente) return;
        modalCliente.classList.add('is-open');
        modalCliente.setAttribute('aria-hidden', 'false');
        document.body.classList.add('fj-modal-open');
        var primero = document.getElementById('nuevoClienteCi');
        if (primero) primero.focus();
    }

    function cerrarNuevoCliente() {
        if (!modalCliente) return;
        modalCliente.classList.remove('is-open');
        modalCliente.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('fj-modal-open');
    }

    if (btnNuevoCliente) {
        btnNuevoCliente.addEventListener('click', abrirNuevoCliente);
    }

    if (modalCliente) {
        modalCliente.addEventListener('click', function (e) {
            if (e.target.classList.contains('fj-modal-backdrop') ||
                e.target.closest('[data-fj-modal-close]')) {
                cerrarNuevoCliente();
            }
        });
    }

    function mostrarErrorCliente(mensaje) {
        errorCliente.replaceChildren(crear('span', null, mensaje));
        errorCliente.hidden = false;
    }

    var campoCliente = {
        Ci: 'nuevoClienteCi',
        Nombre: 'nuevoClienteNombre',
        Apellido: 'nuevoClienteApellido'
    };
    var formatoNombre = /^\p{L}[\p{L}\s.,'\-&()]*$/u;
    var formatoCi = /^[0-9]{6,8}(-[0-9][A-Z])?$/;

    var inputCi = document.getElementById('nuevoClienteCi');
    if (inputCi) {
        inputCi.addEventListener('input', function () {
            inputCi.value = inputCi.value.toUpperCase().replace(/[^0-9A-Z-]/g, '');
        });
    }

    function limpiarErroresCliente() {
        errorCliente.hidden = true;
        errorCliente.replaceChildren();
        Object.keys(campoCliente).forEach(function (clave) {
            var input = document.getElementById(campoCliente[clave]);
            var contenedor = input.closest('.fj-form-field');
            contenedor.classList.remove('fj-form-field-error');
            input.removeAttribute('aria-invalid');
            var aviso = contenedor.querySelector('.fj-form-error');
            if (aviso) aviso.remove();
        });
    }

    function mostrarErroresCampo(errores) {
        var primero = null;
        Object.keys(errores).forEach(function (clave) {
            var id = campoCliente[clave];
            if (!id) return;
            var input = document.getElementById(id);
            var contenedor = input.closest('.fj-form-field');
            contenedor.classList.add('fj-form-field-error');
            input.setAttribute('aria-invalid', 'true');
            var aviso = crear('p', 'fj-form-error', errores[clave]);
            aviso.setAttribute('role', 'alert');
            contenedor.appendChild(aviso);
            if (!primero) primero = input;
        });
        if (primero) primero.focus();
    }

    function validarCliente(datos) {
        var errores = {};
        var nombre = datos.nombre.trim();
        var apellido = datos.apellido.trim();
        var ci = datos.ci.replace(/\s/g, '').toUpperCase();

        if (!ci) errores.Ci = 'El CI del cliente es obligatorio.';
        else if (!formatoCi.test(ci)) errores.Ci = 'El CI debe tener de 6 a 8 dígitos y, si tiene complemento, un guion con un dígito y una letra (ej: 1234567-1A).';

        if (!nombre) errores.Nombre = 'Los nombres del cliente son obligatorios.';
        else if (nombre.length > 100 || !formatoNombre.test(nombre))
            errores.Nombre = 'Los nombres solo admiten letras y espacios (máximo 100 caracteres).';

        if (!apellido) errores.Apellido = 'Los apellidos del cliente son obligatorios.';
        else if (apellido.length > 100 || !formatoNombre.test(apellido))
            errores.Apellido = 'Los apellidos solo admiten letras y espacios (máximo 100 caracteres).';

        return errores;
    }

    if (btnNuevoCliente) {
        btnNuevoCliente.addEventListener('click', limpiarErroresCliente);
    }

    if (formCliente) {
        formCliente.addEventListener('submit', function (e) {
            e.preventDefault();
            limpiarErroresCliente();

            var erroresLocales = validarCliente({
                nombre: document.getElementById('nuevoClienteNombre').value,
                apellido: document.getElementById('nuevoClienteApellido').value,
                ci: document.getElementById('nuevoClienteCi').value
            });
            if (Object.keys(erroresLocales).length > 0) {
                mostrarErroresCampo(erroresLocales);
                return;
            }

            var enviar = document.getElementById('nuevoClienteOk');
            if (enviar) enviar.disabled = true;

            var datos = new FormData(formCliente);

            fetch(document.getElementById('nuevoClienteUrl').value, {
                method: 'POST',
                credentials: 'same-origin',
                body: datos,
                headers: { 'Accept': 'application/json' }
            }).then(leerJson).then(function (respuesta) {
                if (!respuesta.exito) {
                    mostrarErrorCliente(respuesta.mensaje);
                    return;
                }

                mostrarCliente({
                    ci: respuesta.ci,
                    id: respuesta.id,
                    nombre: respuesta.nombre
                });
                cerrarNuevoCliente();
                formCliente.reset();
            }).catch(function (error) {
                if (error.datos && error.datos.errores &&
                    Object.keys(error.datos.errores).length > 0) {
                    mostrarErroresCampo(error.datos.errores);
                    return;
                }
                mostrarErrorCliente(error.message || 'No se pudo registrar el cliente. Intente nuevamente.');
            }).finally(function () {
                if (enviar) enviar.disabled = false;
            });
        });
    }

    // Cerrar resultados al hacer clic fuera
    document.addEventListener('click', function (e) {
        if (!e.target.closest('#buscarCliente') && !e.target.closest('#resultadosCliente')) {
            ocultar(resultadosCliente);
        }
        if (!e.target.closest('#buscarProducto') && !e.target.closest('#resultadosProducto')) {
            ocultar(resultadosProducto);
        }
    });

    render();
})();