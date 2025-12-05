// Map and Marker Logic
var map;
var currentMarker = null;

function initializeMap(geometryWKT) {
    // Standard initialization
    map = L.map('map').setView([60.4720, 8.4689], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);

    // Initial placement based on saved geometry
    if (geometryWKT && geometryWKT.startsWith('POINT')) {
        placeMarkerFromWKT(geometryWKT);
    } else {
        // Fallback: If it's LINESTRING or unknown, center map on Norway
        map.setView([60.4720, 8.4689], 6);
    }

    // Enable placing a new marker by clicking on the map
    map.on('click', function (e) {
        placeMarker(e.latlng);
    });
}

function placeMarkerFromWKT(wkt) {
    // Converts WKT POINT(lng lat) to L.LatLng
    try {
        var coordsString = wkt.replace('POINT(', '').replace(')', '').trim();
        var parts = coordsString.split(' ');
        var lng = parseFloat(parts[0]);
        var lat = parseFloat(parts[1]);

        if (!isNaN(lat) && !isNaN(lng)) {
            var latlng = L.latLng(lat, lng);
            placeMarker(latlng);
            map.setView(latlng, 15);
        }
    } catch (e) {
        console.error('Failed to parse WKT:', e);
    }
}

// Main function to place/move a marker and update the hidden input
function placeMarker(latlng) {
    if (currentMarker) {
        // Move existing marker
        currentMarker.setLatLng(latlng);
    } else {
        // Create new draggable marker
        currentMarker = L.marker(latlng, { draggable: true }).addTo(map);

        // Add drag event listener
        currentMarker.on('dragend', function (e) {
            updateGeometryInput(e.target.getLatLng());
        });
    }

    // Update geometry input immediately
    updateGeometryInput(latlng);
}

function updateGeometryInput(latlng) {
    var wkt = `POINT(${latlng.lng} ${latlng.lat})`;
    document.getElementById('obstacleGeometryInput').value = wkt;
    console.log("New WKT:", wkt);
}

// Toggle custom type input field
function toggleCustomType() {
    var select = document.getElementById('obstacleTypeSelect');
    var customGroup = document.getElementById('customTypeGroup');
    if (select && customGroup) {
        if (select.value === 'Other') {
            customGroup.style.display = 'block';
        } else {
            customGroup.style.display = 'none';
        }
    }
}

// DOM READY: Initialization
document.addEventListener('DOMContentLoaded', function () {
    var mapDiv = document.getElementById('map');
    if (mapDiv) {
        var existingGeometry = mapDiv.getAttribute('data-geometry');
        initializeMap(existingGeometry);
    }

    // Initial check for custom type selection (if coming back with validation errors)
    document.getElementById('obstacleTypeSelect').addEventListener('change', toggleCustomType);
    toggleCustomType();
});