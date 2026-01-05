console.log("Auswertung.js wurde geladen!");

window.renderRatingChart = function (data) {
    console.log("renderRatingChart wurde aufgerufen:", data);

    const ctx = document.getElementById('ratingChart');
    if (!ctx) {
        console.error("Canvas 'ratingChart' nicht gefunden!");
        return;
    }

    // Falls bereits ein Chart existiert → zerstören
    if (window.ratingChartInstance) {
        window.ratingChartInstance.destroy();
    }

    window.ratingChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ["1 Stern", "2 Sterne", "3 Sterne", "4 Sterne", "5 Sterne"],
            datasets: [{
                label: 'Anzahl Bewertungen',
                data: data,
                backgroundColor: [
                    '#d9534f',
                    '#f0ad4e',
                    '#ffd700',
                    '#5bc0de',
                    '#5cb85c'
                ]
            }]
        },
        options: {
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
};