// wwwroot/jsLisa/download.js
window.downloadFileFromBytes = (fileName, bytesBase64) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/pdf;base64," + bytesBase64;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
