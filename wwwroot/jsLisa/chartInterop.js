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
}; //copilot