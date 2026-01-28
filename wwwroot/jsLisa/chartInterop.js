// Chart erstellen
window.createChart = function (canvasId, chartType, data, options) {
    const ctx = document.getElementById(canvasId);

    if (!ctx) return;

    if (ctx.chartInstance) {
        ctx.chartInstance.destroy();
    }

    ctx.chartInstance = new Chart(ctx, {
        type: chartType,
        data: data,
        options: options
    });
};

// Bild exportieren – UNZERSTÖRBAR
window.getChartImage = function (canvasId) {
    const ctx = document.getElementById(canvasId);
    if (!ctx || !ctx.chartInstance) return null;
    return ctx.chartInstance.toBase64Image();
};

// PDF Download
window.downloadFileFromBytes = (fileName, base64) => {
    const link = document.createElement('a');
    link.href = "data:application/pdf;base64," + base64;
    link.download = fileName;
    link.click();
};