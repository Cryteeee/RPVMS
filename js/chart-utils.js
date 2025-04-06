// Wait for Chart.js to be fully loaded
window.initializeChartJs = () => {
    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded');
        return;
    }

    // Register Chart.js plugins
    if (typeof ChartDataLabels !== 'undefined') {
        Chart.register(ChartDataLabels);
    }

    // Set default options for all charts
    Chart.defaults.font.family = "'Inter', 'Helvetica', 'Arial', sans-serif";
    Chart.defaults.scale.grid.display = false;
    Chart.defaults.plugins.tooltip.padding = 10;
    Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(0, 0, 0, 0.8)';
    Chart.defaults.plugins.tooltip.titleColor = '#ffffff';
    Chart.defaults.plugins.tooltip.bodyColor = '#ffffff';
    Chart.defaults.plugins.tooltip.borderWidth = 0;
    Chart.defaults.plugins.tooltip.borderRadius = 4;
};

// Initialize Chart.js when the script loads
window.initializeChartJs();

let chartInstance = null;

window.initializeChart = (canvasId, config) => {
    try {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Canvas element not found:', canvasId);
            return;
        }

        // Destroy existing chart if it exists
        if (chartInstance) {
            chartInstance.destroy();
        }

        // Process callback functions that were passed as strings
        if (config.options?.scales?.y?.ticks?.callback && typeof config.options.scales.y.ticks.callback === 'string') {
            config.options.scales.y.ticks.callback = new Function('value', 'return "₱" + value.toLocaleString();');
        }

        if (config.options?.plugins?.tooltip?.callbacks?.label && typeof config.options.plugins.tooltip.callbacks.label === 'string') {
            config.options.plugins.tooltip.callbacks.label = function(context) {
                return context.dataset.label + ': ₱' + context.raw.toLocaleString();
            };
        }

        // Create new chart
        chartInstance = new Chart(canvas.getContext('2d'), {
            type: config.type,
            data: config.data,
            options: {
                ...config.options,
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    ...config.options.plugins,
                    legend: {
                        display: false
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            maxRotation: 45,
                            minRotation: 45
                        }
                    },
                    y: {
                        beginAtZero: true,
                        grid: {
                            borderDash: [2, 4]
                        },
                        ticks: {
                            callback: function(value) {
                                return '₱' + value.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error initializing chart:', error);
    }
}; 