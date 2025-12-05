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
        function (position) {
            var lat = position.coords.latitude;
            var lng = position.coords.longitude;

            // Place marker using the global function
            if (window.placeMarkerOnMap) {
                window.placeMarkerOnMap(lat, lng);
            }

            btn.disabled = false;
            btn.textContent = 'Use My Location';
        },
        function (error) {
            btn.disabled = false;
            btn.textContent = 'Use My Location';
            alert('Could not get location. Ensure permissions are granted.');
        },
        { enableHighAccuracy: true }
    );
}

// Initialize map after DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Leaflet map
    map = L.map('map').setView([60.4720, 8.4689], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);

    drawnItems = new L.FeatureGroup();
    map.addLayer(drawnItems);

    // Add draw control with Marker AND Polyline (Line)
    var drawControl = new L.Control.Draw({
        edit: {
            featureGroup: drawnItems,
            edit: false, // Forenkling: Tegn på nytt hvis feil, i stedet for edit
            remove: true
        },
        draw: {
            polygon: false,
            rectangle: false,
            circle: false,
            circlemarker: false,

            // AKTIVER LINJETEGNING HER:
            polyline: {
                shapeOptions: {
                    color: '#2563eb', // Blue
                    weight: 4
                }
            },
            marker: {
                icon: new L.Icon.Default()
            }
        }
    });
    map.addControl(drawControl);

    // Hjelpefunksjon: Lag WKT-streng basert på laget
    function updateGeometryInput(layer, type) {
        var wkt = "";

        if (type === 'marker') {
            var pos = layer.getLatLng();
            // WKT Format: POINT(lng lat)
            wkt = `POINT(${pos.lng} ${pos.lat})`;
        }
        else if (type === 'polyline') {
            var latlngs = layer.getLatLngs();
            // WKT Format: LINESTRING(lng1 lat1, lng2 lat2, ...)
            var coords = latlngs.map(function (ll) {
                return `${ll.lng} ${ll.lat}`;
            }).join(', ');
            wkt = `LINESTRING(${coords})`;
        }

        console.log("Generated WKT:", wkt); // Debugging
        document.getElementById('obstacleGeometryInput').value = wkt;
    }

    // Handle items created from toolbar (Marker or Line)
    map.on(L.Draw.Event.CREATED, function (event) {
        var layer = event.layer;
        var type = event.layerType;

        // Clear existing items (vi tillater bare én figur om gangen)
        drawnItems.clearLayers();
        drawnItems.addLayer(layer);

        // Hvis det er en markør, gjør den flyttbar
        if (type === 'marker') {
            layer.dragging.enable();
            layer.on('dragend', function (e) {
                updateGeometryInput(e.target, 'marker');
            });
        }

        // Oppdater input-feltet
        updateGeometryInput(layer, type);
    });

    // Handle deletion
    map.on(L.Draw.Event.DELETED, function (e) {
        document.getElementById('obstacleGeometryInput').value = '';
    });

    // Click on map short-cut: Only place marker if NOT drawing a line
    map.on('click', function (e) {
        // Sjekk om vi driver og tegner en linje (draw handler active)
        // Dette hindrer at vi setter ut punkter mens vi tegner linjer
        if (drawControl._toolbars.draw._modes.polyline && drawControl._toolbars.draw._modes.polyline.handler.enabled()) {
            return;
        }
        drawnItems.clearLayers();
        var marker = L.marker(e.latlng, { draggable: true }).addTo(drawnItems);

        marker.on('dragend', function (ev) {
            updateGeometryInput(ev.target, 'marker');
        });

        updateGeometryInput(marker, 'marker');
    });

    // Global function for "Use My Location" button
    window.placeMarkerOnMap = function (lat, lng) {
        var latlng = L.latLng(lat, lng);
        drawnItems.clearLayers();

        var marker = L.marker(latlng, { draggable: true }).addTo(drawnItems);
        map.setView(latlng, 15);

        marker.on('dragend', function (ev) {
            updateGeometryInput(ev.target, 'marker');
        });

        updateGeometryInput(marker, 'marker');
    };

    // Attach event listeners
    document.getElementById('useLocationBtn').addEventListener('click', useMyLocation);
    document.getElementById('obstacleTypeSelect').addEventListener('change', toggleCustomType);

    // Form submit handler (Custom type logic)
    document.querySelector('form').addEventListener('submit', function (e) {
        var select = document.getElementById('obstacleTypeSelect');
        var customInput = document.getElementById('customTypeName');

        if (select.value === 'Other' && customInput.value.trim()) {
            var hiddenInput = document.createElement('input');
            hiddenInput.type = 'hidden';
            hiddenInput.name = 'CustomObstacleType';
            hiddenInput.value = customInput.value.trim();
            this.appendChild(hiddenInput);
        }
    });
});