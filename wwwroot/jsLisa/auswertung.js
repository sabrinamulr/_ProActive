console.log("Auswertung.js wurde geladen!");

window.renderRatingChart = function (data) {
    console.log("renderRatingChart wurde aufgerufen:", data);

    const canvas = document.getElementById('ratingChart');
    if (!canvas) {
        console.error("Canvas 'ratingChart' nicht gefunden!");
        return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
        console.error("Kein 2D-Kontext für Canvas gefunden!");
        return;
    }

    // TEST: Einfach nur den Hintergrund rot füllen, ohne Chart.js
    ctx.fillStyle = 'red';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    console.log("Test-Rechteck gezeichnet.");
};