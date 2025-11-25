// Global variables for map access
var map;
var drawnItems;

// Toggle custom type input field
function toggleCustomType() {
    var select = document.getElementById('obstacleTypeSelect');
    var customGroup = document.getElementById('customTypeGroup');
    var customInput = document.getElementById('customTypeName');

    if (select.value === 'Other') {
        customGroup.style.display = 'block';
        customInput.required = true;
    } else {
        customGroup.style.display = 'none';
        customInput.required = false;
        customInput.value = '';
    }
}

// Use device GPS location
function useMyLocation() {
    var btn = document.getElementById('useLocationBtn');

    if (!navigator.geolocation) {
        alert('Geolocation is not supported by your browser');
        return;
    }

    btn.disabled = true;
    btn.textContent = 'Getting location...';

    navigator.geolocation.getCurrentPosition(
        function(position) {
            var lat = position.coords.latitude;
            var lng = position.coords.longitude;

            // Place marker using the global function
            if (window.placeMarkerOnMap) {
                window.placeMarkerOnMap(lat, lng);
            }

            btn.disabled = false;
            btn.textContent = 'Use My Location';
        },
        function(error) {
            btn.disabled = false;
            btn.textContent = 'Use My Location';

            switch(error.code) {
                case error.PERMISSION_DENIED:
                    alert('Location access denied. Please enable location permissions.');
                    break;
                case error.POSITION_UNAVAILABLE:
                    alert('Location information is unavailable.');
                    break;
                case error.TIMEOUT:
                    alert('Location request timed out.');
                    break;
                default:
                    alert('An unknown error occurred getting your location.');
                    break;
            }
        },
        {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 0
        }
    );
}

// Initialize map after DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize Leaflet map
    map = L.map('map').setView([60.4720, 8.4689], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);

    drawnItems = new L.FeatureGroup();
    map.addLayer(drawnItems);

    var currentMarker = null;

    // Add draw control with only marker tool
    var drawControl = new L.Control.Draw({
        edit: {
            featureGroup: drawnItems,
            edit: false,
            remove: false
        },
        draw: {
            polygon: false,
            polyline: false,
            rectangle: false,
            circle: false,
            circlemarker: false,
            marker: {
                icon: new L.Icon.Default()
            }
        }
    });
    map.addControl(drawControl);

    // Function to place or move marker
    function placeMarker(latlng) {
        if (currentMarker) {
            // Move existing marker
            currentMarker.setLatLng(latlng);
        } else {
            // Create new draggable marker
            currentMarker = L.marker(latlng, { draggable: true }).addTo(drawnItems);

            // Update coordinates when marker is dragged
            currentMarker.on('dragend', function(e) {
                var pos = e.target.getLatLng();
                document.getElementById('obstacleGeometryInput').value = `POINT(${pos.lng} ${pos.lat})`;
            });
        }

        // Update geometry input
        document.getElementById('obstacleGeometryInput').value = `POINT(${latlng.lng} ${latlng.lat})`;
    }

    // Handle marker created from toolbar
    map.on(L.Draw.Event.CREATED, function (event) {
        var layer = event.layer;

        // Remove existing marker if any
        if (currentMarker) {
            drawnItems.removeLayer(currentMarker);
        }

        // Make the new marker draggable
        layer.options.draggable = true;
        if (layer.dragging) {
            layer.dragging.enable();
        }

        drawnItems.addLayer(layer);
        currentMarker = layer;

        // Update coordinates when marker is dragged
        currentMarker.on('dragend', function(e) {
            var pos = e.target.getLatLng();
            document.getElementById('obstacleGeometryInput').value = `POINT(${pos.lng} ${pos.lat})`;
        });

        // Set geometry input
        var latlng = layer.getLatLng();
        document.getElementById('obstacleGeometryInput').value = `POINT(${latlng.lng} ${latlng.lat})`;
    });

    // Click on map to place/move marker
    map.on('click', function(e) {
        placeMarker(e.latlng);
    });

    // Make placeMarker available globally for useMyLocation
    window.placeMarkerOnMap = function(lat, lng) {
        var latlng = L.latLng(lat, lng);
        placeMarker(latlng);
        map.setView(latlng, 15);
    };

    // Attach event listeners instead of inline handlers
    document.getElementById('useLocationBtn').addEventListener('click', useMyLocation);
    document.getElementById('obstacleTypeSelect').addEventListener('change', toggleCustomType);

    // Update obstacle type with custom value before form submit
    document.querySelector('form').addEventListener('submit', function(e) {
        var select = document.getElementById('obstacleTypeSelect');
        var customInput = document.getElementById('customTypeName');

        if (select.value === 'Other' && customInput.value.trim()) {
            // Create a hidden input with the custom type value
            var hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = 'CustomObstacleType';
            hiddenInput.value = customInput.value.trim();
            this.appendChild(hiddenInput);
        }
    });
});
