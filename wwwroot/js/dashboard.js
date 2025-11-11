// BateauxERP Dashboard Charts
// Using ApexCharts library

document.addEventListener('DOMContentLoaded', function() {

    // Initialize filter controls
    initFilterControls();

    // Initialize all charts with a small delay to ensure data is loaded
    setTimeout(function() {
        initProductionChart();
        initRevenueChart();
        initConsommablesChart();
        initDailyActivityChart();
    }, 100);

});

// Initialize Filter Controls
function initFilterControls() {
    const dateRangeSelect = document.getElementById('dateRangeSelect');
    const customStartDateDiv = document.getElementById('customStartDateDiv');
    const customEndDateDiv = document.getElementById('customEndDateDiv');
    const bateauSelectCol = document.querySelector('[for="bateauSelect"]')?.closest('.col-md-3');
    const periodSelectCol = document.querySelector('[for="dateRangeSelect"]')?.closest('.col-md-3');

    if (!dateRangeSelect) return;

    // Function to toggle custom date inputs
    function toggleCustomDateInputs() {
        const selectedValue = dateRangeSelect.value;

        if (selectedValue === 'custom') {
            customStartDateDiv.style.display = 'block';
            customEndDateDiv.style.display = 'block';

            // Adjust column widths
            if (bateauSelectCol) bateauSelectCol.classList.replace('col-md-3', 'col-md-2');
            if (periodSelectCol) periodSelectCol.classList.replace('col-md-3', 'col-md-2');
        } else {
            customStartDateDiv.style.display = 'none';
            customEndDateDiv.style.display = 'none';

            // Reset column widths
            if (bateauSelectCol) bateauSelectCol.classList.replace('col-md-2', 'col-md-3');
            if (periodSelectCol) periodSelectCol.classList.replace('col-md-2', 'col-md-3');
        }
    }

    // Initial check on page load
    toggleCustomDateInputs();

    // Listen for changes
    dateRangeSelect.addEventListener('change', toggleCustomDateInputs);
}

// Production Trends Chart
function initProductionChart() {
    // Check if element exists
    const chartElement = document.querySelector("#productionChart");
    if (!chartElement) {
        console.log('Production chart element not found');
        return;
    }

    // Check if data exists and is valid
    if (!window.productionData || !Array.isArray(window.productionData) || window.productionData.length === 0) {
        console.log('No production data available');
        return;
    }

    var options = {
        series: [{
            name: 'Production (KG)',
            data: window.productionData.map(item => item.totalProductionKG || 0)
        }, {
            name: 'Nombre de Marées',
            data: window.productionData.map(item => item.mareesCount || 0)
        }],
        chart: {
            type: 'line',
            height: 350,
            toolbar: {
                show: true,
                tools: {
                    download: true,
                    selection: false,
                    zoom: false,
                    zoomin: false,
                    zoomout: false,
                    pan: false,
                    reset: false
                }
            }
        },
        colors: ['#0d6efd', '#20c997'],
        dataLabels: {
            enabled: false
        },
        stroke: {
            curve: 'smooth',
            width: 3
        },
        xaxis: {
            categories: window.productionData.map(item => item.month || ''),
            labels: {
                style: {
                    fontSize: '12px'
                }
            }
        },
        yaxis: [{
            title: {
                text: 'Production (KG)',
                style: {
                    fontSize: '13px',
                    fontWeight: 600
                }
            },
            labels: {
                formatter: function(val) {
                    return val.toFixed(0);
                }
            }
        }, {
            opposite: true,
            title: {
                text: 'Nombre de Marées',
                style: {
                    fontSize: '13px',
                    fontWeight: 600
                }
            },
            labels: {
                formatter: function(val) {
                    return val.toFixed(0);
                }
            }
        }],
        legend: {
            position: 'top',
            horizontalAlign: 'right'
        },
        tooltip: {
            shared: true,
            intersect: false,
            y: [{
                formatter: function(val) {
                    return val.toFixed(0) + ' KG';
                }
            }, {
                formatter: function(val) {
                    return val + ' marées';
                }
            }]
        },
        grid: {
            borderColor: '#f1f1f1',
            strokeDashArray: 4
        }
    };

    var chart = new ApexCharts(document.querySelector("#productionChart"), options);
    chart.render();
}

// Revenue Analysis Chart
function initRevenueChart() {
    // Check if element exists
    const chartElement = document.querySelector("#revenueChart");
    if (!chartElement) {
        console.log('Revenue chart element not found');
        return;
    }

    // Check if data exists and is valid
    if (!window.revenueData || !Array.isArray(window.revenueData) || window.revenueData.length === 0) {
        console.log('No revenue data available');
        return;
    }

    var options = {
        series: [{
            name: 'Revenu Brut',
            data: window.revenueData.map(item => item.revenue || 0)
        }, {
            name: 'Taxes Portuaires',
            data: window.revenueData.map(item => item.portTaxes || 0)
        }, {
            name: 'Revenu Net',
            data: window.revenueData.map(item => item.netRevenue || 0)
        }],
        chart: {
            type: 'bar',
            height: 350,
            toolbar: {
                show: true
            },
            stacked: false
        },
        colors: ['#0d6efd', '#dc3545', '#198754'],
        plotOptions: {
            bar: {
                horizontal: false,
                columnWidth: '55%',
                borderRadius: 4
            }
        },
        dataLabels: {
            enabled: false
        },
        stroke: {
            show: true,
            width: 2,
            colors: ['transparent']
        },
        xaxis: {
            categories: window.revenueData.map(item => item.month || ''),
            labels: {
                style: {
                    fontSize: '12px'
                }
            }
        },
        yaxis: {
            title: {
                text: 'Montant (DHS)',
                style: {
                    fontSize: '13px',
                    fontWeight: 600
                }
            },
            labels: {
                formatter: function(val) {
                    return val.toFixed(0);
                }
            }
        },
        fill: {
            opacity: 1
        },
        legend: {
            position: 'top',
            horizontalAlign: 'right'
        },
        tooltip: {
            shared: true,
            intersect: false,
            y: {
                formatter: function(val) {
                    return val.toFixed(2) + ' DHS';
                }
            }
        },
        grid: {
            borderColor: '#f1f1f1',
            strokeDashArray: 4
        }
    };

    var chart = new ApexCharts(document.querySelector("#revenueChart"), options);
    chart.render();
}

// Consumables Distribution Chart (Donut)
function initConsommablesChart() {
    // Check if element exists
    const chartElement = document.querySelector("#consommablesChart");
    if (!chartElement) {
        console.log('Consommables chart element not found');
        return;
    }

    // Check if data exists and is valid
    if (!window.consommablesData || !Array.isArray(window.consommablesData) || window.consommablesData.length === 0) {
        console.log('No consommables data available');
        return;
    }

    var options = {
        series: window.consommablesData.map(item => item.totalQuantity || 0),
        chart: {
            type: 'donut',
            height: 300
        },
        labels: window.consommablesData.map(item => item.typeConsommable || 'N/A'),
        colors: ['#0d6efd', '#198754', '#ffc107', '#dc3545', '#0dcaf0', '#6610f2'],
        legend: {
            position: 'bottom',
            fontSize: '12px'
        },
        plotOptions: {
            pie: {
                donut: {
                    size: '70%',
                    labels: {
                        show: true,
                        name: {
                            show: true,
                            fontSize: '14px'
                        },
                        value: {
                            show: true,
                            fontSize: '16px',
                            fontWeight: 600,
                            formatter: function(val) {
                                return parseFloat(val).toFixed(0);
                            }
                        },
                        total: {
                            show: true,
                            label: 'Total',
                            fontSize: '14px',
                            formatter: function(w) {
                                return w.globals.seriesTotals.reduce((a, b) => a + b, 0).toFixed(0);
                            }
                        }
                    }
                }
            }
        },
        dataLabels: {
            enabled: false
        },
        tooltip: {
            y: {
                formatter: function(val, opts) {
                    var unite = window.consommablesData[opts.seriesIndex]?.unite || '';
                    return val.toFixed(0) + ' ' + unite;
                }
            }
        }
    };

    var chart = new ApexCharts(document.querySelector("#consommablesChart"), options);
    chart.render();
}

// Daily Activity Chart (Last 30 Days)
function initDailyActivityChart() {
    // Check if element exists
    const chartElement = document.querySelector("#dailyActivityChart");
    if (!chartElement) {
        console.log('Daily activity chart element not found');
        return;
    }

    // Check if data exists and is valid
    if (!window.dailyData || !Array.isArray(window.dailyData) || window.dailyData.length === 0) {
        console.log('No daily activity data available');
        return;
    }

    // Process dates for better display
    var categories = window.dailyData.map(item => {
        try {
            var date = new Date(item.date);
            return date.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' });
        } catch (e) {
            return '';
        }
    });

    var options = {
        series: [{
            name: 'Marées',
            data: window.dailyData.map(item => item.count || 0)
        }],
        chart: {
            type: 'area',
            height: 300,
            toolbar: {
                show: false
            },
            sparkline: {
                enabled: false
            }
        },
        colors: ['#0d6efd'],
        dataLabels: {
            enabled: false
        },
        stroke: {
            curve: 'smooth',
            width: 2
        },
        fill: {
            type: 'gradient',
            gradient: {
                shadeIntensity: 1,
                opacityFrom: 0.7,
                opacityTo: 0.2,
                stops: [0, 90, 100]
            }
        },
        xaxis: {
            categories: categories,
            labels: {
                rotate: -45,
                style: {
                    fontSize: '10px'
                },
                showDuplicates: false
            },
            tickAmount: 10
        },
        yaxis: {
            title: {
                text: 'Nombre de Marées',
                style: {
                    fontSize: '13px',
                    fontWeight: 600
                }
            },
            labels: {
                formatter: function(val) {
                    return val.toFixed(0);
                }
            }
        },
        tooltip: {
            x: {
                format: 'dd MMM yyyy'
            },
            y: {
                formatter: function(val) {
                    return val + ' marée(s)';
                }
            }
        },
        grid: {
            borderColor: '#f1f1f1',
            strokeDashArray: 4
        }
    };

    var chart = new ApexCharts(document.querySelector("#dailyActivityChart"), options);
    chart.render();
}

// Helper function to format numbers
function formatNumber(num) {
    return num.toLocaleString('fr-FR');
}

// Helper function to format currency
function formatCurrency(num) {
    return num.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' DHS';
}
