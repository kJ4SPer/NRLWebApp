document.addEventListener('DOMContentLoaded', function() {
    var map = L.map('map').setView([60.4720, 8.4689], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);

    // Add event listeners for confirmation dialogs
    const approveBtn = document.getElementById('btn-approve-obstacle');
    if (approveBtn) {
        approveBtn.addEventListener('click', function(e) {
            if (!confirm('Are you sure you want to approve this obstacle?')) {
                e.preventDefault();
            }
        });
    }

    const rejectBtn = document.getElementById('btn-reject-obstacle');
    if (rejectBtn) {
        rejectBtn.addEventListener('click', function(e) {
            if (!confirm('Are you sure you want to reject this obstacle?')) {
                e.preventDefault();
            }
        });
    }

    // Get geometry and name from data attributes
    var geometryWKT = document.getElementById('map').getAttribute('data-geometry');
    var obstacleName = document.getElementById('map').getAttribute('data-name');
    var obstacleHeight = document.getElementById('map').getAttribute('data-height');

    if (geometryWKT) {
        try {
            if (geometryWKT.startsWith('POINT')) {
                var coords = geometryWKT.replace('POINT(', '').replace(')', '').split(' ');
                var lat = parseFloat(coords[1]);
                var lng = parseFloat(coords[0]);

                var marker = L.marker([lat, lng]).addTo(map);
                marker.bindPopup('<b>' + obstacleName + '</b><br>' + obstacleHeight + ' m').openPopup();
                map.setView([lat, lng], 13);
            } else if (geometryWKT.startsWith('LINESTRING')) {
                var coordsStr = geometryWKT.replace('LINESTRING(', '').replace(')', '');
                var coordPairs = coordsStr.split(',');
                var latlngs = coordPairs.map(pair => {
                    var coords = pair.trim().split(' ');
                    return [parseFloat(coords[1]), parseFloat(coords[0])];
                });

                var polyline = L.polyline(latlngs, {color: 'blue'}).addTo(map);
                polyline.bindPopup('<b>' + obstacleName + '</b><br>' + obstacleHeight + ' m');
                map.fitBounds(polyline.getBounds());
            } else if (geometryWKT.startsWith('POLYGON')) {
                var coordsStr = geometryWKT.replace('POLYGON((', '').replace('))', '');
                var coordPairs = coordsStr.split(',');
                var latlngs = coordPairs.map(pair => {
                    var coords = pair.trim().split(' ');
                    return [parseFloat(coords[1]), parseFloat(coords[0])];
                });

                var polygon = L.polygon(latlngs, {color: 'blue'}).addTo(map);
                polygon.bindPopup('<b>' + obstacleName + '</b><br>' + obstacleHeight + ' m');
                map.fitBounds(polygon.getBounds());
            }
        } catch (e) {
            console.error('Error parsing geometry:', e);
        }
    }
});
