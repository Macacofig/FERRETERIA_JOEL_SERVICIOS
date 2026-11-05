// ============================================================
// FERRETERÍA JOEL · Gráfico del informe estadístico
// Lee los datos que Razor serializa en un <script type="application/json">
// y dibuja una gráfica de torta con Chart.js.
// ============================================================

(function () {
    'use strict';

    const formatoMoneda = new Intl.NumberFormat('es-BO', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const formatoCompacto = new Intl.NumberFormat('es-BO', {
        notation: 'compact',
        maximumFractionDigits: 1
    });

    function leerDatos() {
        const nodo = document.getElementById('reportesDatosGrafico');

        if (!nodo || !nodo.textContent) {
            return null;
        }

        try {
            return JSON.parse(nodo.textContent);
        } catch (error) {
            console.error('No se pudieron leer los datos del gráfico.', error);
            return null;
        }
    }

    function mostrarSinDatos(canvas) {
        const aviso = document.getElementById('reportesGraficoSinDatos');

        canvas.hidden = true;

        if (aviso) {
            aviso.hidden = false;
        }
    }

    function dibujarGrafico() {
        const canvas = document.getElementById('reportesGrafico');

        if (!canvas) {
            return;
        }

        const datos = leerDatos();
        const etiquetas = datos && Array.isArray(datos.etiquetas) ? datos.etiquetas : [];
        const valores = datos && Array.isArray(datos.valores) ? datos.valores : [];

        if (etiquetas.length === 0 || valores.length === 0 || typeof Chart === 'undefined') {
            mostrarSinDatos(canvas);
            return;
        }

        new Chart(canvas, {
            type: 'pie',
            data: {
                labels: etiquetas,
                datasets: [{
                    label: 'Recaudación (Bs)',
                    data: valores,
                    backgroundColor: ['#eab308', '#2563eb', '#16a34a', '#dc2626', '#9333ea', '#0891b2', '#ea580c', '#4f46e5', '#65a30d', '#db2777'],
                    borderColor: '#ffffff',
                    borderWidth: 2
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 450
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(29, 29, 31, 0.92)',
                        padding: 12,
                        cornerRadius: 8,
                        displayColors: false,
                        callbacks: {
                            label: function (contexto) {
                                return 'Bs ' + formatoMoneda.format(contexto.parsed.x);
                            }
                        }
                    }
                },
                scales: {}
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', dibujarGrafico);
    } else {
        dibujarGrafico();
    }
})();
