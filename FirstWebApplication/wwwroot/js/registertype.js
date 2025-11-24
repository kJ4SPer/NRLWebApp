// RegisterType.cshtml specific JavaScript

// Initialize the map when the page loads
document.addEventListener('DOMContentLoaded', function () {

    // Create the map centered on Norway
    // The map is set to not be interactive
    var map = L.map('register-type-map', {
        center: [60.4720, 8.4689],
        zoom: 5,
        zoomControl: false,
        dragging: false,
        touchZoom: false,
        scrollWheelZoom: false,
        doubleClickZoom: false,
        boxZoom: false,
        keyboard: false
    });

    // Add the OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);
});
