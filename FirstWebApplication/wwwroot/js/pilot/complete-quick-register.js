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

// Add event listener for select change
document.addEventListener('DOMContentLoaded', function() {
    var select = document.getElementById('obstacleTypeSelect');
    if (select) {
        select.addEventListener('change', toggleCustomType);
    }
});

// Update obstacle type with custom value before form submit
document.querySelector('form').addEventListener('submit', function(e) {
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

document.addEventListener('DOMContentLoaded', function() {
    // Initialize map
    var map = L.map('mapid').setView([60.4720, 8.4689], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);

    // Get saved geometry from data attribute
    var geometryWKT = document.getElementById('mapid').getAttribute('data-geometry');

    if (geometryWKT) {
        try {
            if (geometryWKT.startsWith('POINT')) {
                var coords = geometryWKT.replace('POINT(', '').replace(')', '').split(' ');
                var lat = parseFloat(coords[1]);
                var lng = parseFloat(coords[0]);

                var marker = L.marker([lat, lng]).addTo(map);
                map.setView([lat, lng], 13);
            } else if (geometryWKT.startsWith('LINESTRING')) {
                var coordsStr = geometryWKT.replace('LINESTRING(', '').replace(')', '');
                var coordPairs = coordsStr.split(',');
                var latlngs = coordPairs.map(pair => {
                    var coords = pair.trim().split(' ');
                    return [parseFloat(coords[1]), parseFloat(coords[0])];
                });

                var polyline = L.polyline(latlngs, {color: 'blue'}).addTo(map);
                map.fitBounds(polyline.getBounds());
            } else if (geometryWKT.startsWith('POLYGON')) {
                var coordsStr = geometryWKT.replace('POLYGON((', '').replace('))', '');
                var coordPairs = coordsStr.split(',');
                var latlngs = coordPairs.map(pair => {
                    var coords = pair.trim().split(' ');
                    return [parseFloat(coords[1]), parseFloat(coords[0])];
                });

                var polygon = L.polygon(latlngs, {color: 'blue'}).addTo(map);
                map.fitBounds(polygon.getBounds());
            }
        } catch (e) {
            console.error('Error parsing geometry:', e);
        }
    }
});
