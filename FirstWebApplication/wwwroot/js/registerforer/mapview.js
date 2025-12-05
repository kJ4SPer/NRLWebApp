// MapView-specific JavaScript for obstacle map functionality

let map;
let allObstacles = [];
let markersLayer;
let searchTimeout;
let availableTypes = [];

document.addEventListener('DOMContentLoaded', function () {
    console.log('🗺️ Initialiserer Map View...');
    initializeMap();
    loadObstacles();
    setupInitialEventListeners();
});

function initializeMap() {
    map = L.map('map').setView([60.4720, 8.4689], 6);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(map);
    markersLayer = L.layerGroup().addTo(map);
    console.log('✅ Kart initialisert');
}

async function loadObstacles() {
    try {
        console.log('📡 Henter obstacles...');
        const response = await fetch(window.mapViewConfig.getObstaclesUrl);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log('✅ Data mottatt:', data);

        allObstacles = data.obstacles;
        updateStatistics(data.stats);
        buildTypeFilters();
        displayObstacles(allObstacles);

        document.getElementById('loading-overlay').style.display = 'none';
        console.log(`📍 ${allObstacles.length} obstacles lastet!`);
    } catch (error) {
        console.error('❌ Feil ved lasting av obstacles:', error);
        const loadingOverlay = document.getElementById('loading-overlay');
        loadingOverlay.innerHTML =
            `<div style="text-align: center; color: #ef4444;">
                <h3>⚠️ Failed to load obstacles</h3>
                <p>${error.message}</p>
                <button id="retry-btn" style="margin-top: 20px; padding: 10px 20px; background: #667eea; color: white; border: none; border-radius: 8px; cursor: pointer;">
                    🔄 Retry
                </button>
            </div>`;
        document.getElementById('retry-btn').addEventListener('click', function () {
            location.reload();
        });
    }
}

function updateStatistics(stats) {
    document.getElementById('stat-total').textContent = stats.total;
    document.getElementById('stat-approved').textContent = stats.approved;
    document.getElementById('stat-pending').textContent = stats.pending;
    document.getElementById('stat-rejected').textContent = stats.rejected;
}

function buildTypeFilters() {
    availableTypes = [...new Set(allObstacles.map(o => o.type))].sort();
    console.log('🏗️ Tilgjengelige typer:', availableTypes);

    const typeFilterSection = document.getElementById('type-filter-section');
    const existingCheckboxes = typeFilterSection.querySelectorAll('.type-checkbox');
    existingCheckboxes.forEach(cb => cb.parentElement.remove());

    availableTypes.forEach((type, index) => {
        const div = document.createElement('div');
        div.className = 'filter-option';

        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.id = `filter-type-${index}`;
        checkbox.className = 'type-checkbox';
        checkbox.dataset.type = type;
        checkbox.addEventListener('change', handleTypeFilterChange);

        const label = document.createElement('label');
        label.htmlFor = `filter-type-${index}`;
        label.textContent = type;

        div.appendChild(checkbox);
        div.appendChild(label);
        typeFilterSection.appendChild(div);
    });

    console.log(`✅ Bygget ${availableTypes.length} type-filtre`);
}

function displayObstacles(obstacles) {
    markersLayer.clearLayers();
    console.log(`🎨 Viser ${obstacles.length} markers...`);

    obstacles.forEach(obstacle => {
        try {
            const geometryWKT = obstacle.geometry;

            if (!geometryWKT) {
                console.warn(`⚠️ Obstacle ${obstacle.id} har ingen geometry`);
                return;
            }

            let markerColor = '#6b7280';
            let statusText = 'Unknown';

            if (obstacle.isApproved) {
                markerColor = '#10b981';
                statusText = 'Approved';
            } else if (obstacle.isPending) {
                markerColor = '#f59e0b';
                statusText = 'Pending';
            } else if (obstacle.isRejected) {
                markerColor = '#ef4444';
                statusText = 'Rejected';
            }

            const icon = L.divIcon({
                html: `<div style="background-color: ${markerColor}; width: 24px; height: 24px; border-radius: 50%; border: 3px solid white; box-shadow: 0 2px 4px rgba(0,0,0,0.3);"></div>`,
                iconSize: [24, 24],
                className: ''
            });

            // ENDRET: Bruker ID i tittelen i stedet for Navn
            const titleHtml = `<h3 style="margin: 0 0 10px 0; color: #1f2937; font-size: 16px;">Obstacle #${obstacle.id}</h3>`;

            if (geometryWKT.startsWith('POINT')) {
                const coords = geometryWKT.replace('POINT(', '').replace(')', '').split(' ');
                const lat = parseFloat(coords[1]);
                const lng = parseFloat(coords[0]);

                const marker = L.marker([lat, lng], { icon: icon }).addTo(markersLayer);

                const popupContent = `
                    <div style="min-width: 200px;">
                        ${titleHtml}
                        <p style="margin: 5px 0;"><strong>Type:</strong> ${obstacle.type}</p>
                        <p style="margin: 5px 0;"><strong>Height:</strong> ${obstacle.height}m</p>
                        <p style="margin: 5px 0;"><strong>Status:</strong> <span style="color: ${markerColor}; font-weight: bold;">${statusText}</span></p>
                        <p style="margin: 5px 0;"><strong>Registered by:</strong> ${obstacle.registeredBy}</p>
                        ${obstacle.description ? `<p style="margin: 10px 0 0 0; font-size: 13px; color: #6b7280;">${obstacle.description}</p>` : ''}
                        <a href="${window.mapViewConfig.viewObstacleUrl}?id=${obstacle.id}" 
                           style="display: inline-block; margin-top: 10px; padding: 6px 12px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; font-size: 13px;">
                            View Details →
                        </a>
                    </div>
                `;

                marker.bindPopup(popupContent);

            } else if (geometryWKT.startsWith('LINESTRING')) {
                const coordsStr = geometryWKT.replace('LINESTRING(', '').replace(')', '');
                const coordPairs = coordsStr.split(',');
                const latlngs = coordPairs.map(pair => {
                    const coords = pair.trim().split(' ');
                    return [parseFloat(coords[1]), parseFloat(coords[0])];
                });

                const polyline = L.polyline(latlngs, { color: markerColor, weight: 4 }).addTo(markersLayer);

                const popupContent = `
                    <div>
                        ${titleHtml}
                        <p><strong>Type:</strong> ${obstacle.type}</p>
                        <p><strong>Status:</strong> <span style="color: ${markerColor};">${statusText}</span></p>
                        <a href="${window.mapViewConfig.viewObstacleUrl}?id=${obstacle.id}">View Details</a>
                    </div>
                `;

                polyline.bindPopup(popupContent);

            } else if (geometryWKT.startsWith('POLYGON')) {
                const coordsStr = geometryWKT.replace('POLYGON((', '').replace('))', '');
                const coordPairs = coordsStr.split(',');
                const latlngs = coordPairs.map(pair => {
                    const coords = pair.trim().split(' ');
                    return [parseFloat(coords[1]), parseFloat(coords[0])];
                });

                const polygon = L.polygon(latlngs, { color: markerColor, fillColor: markerColor, fillOpacity: 0.3 }).addTo(markersLayer);

                const popupContent = `
                    <div>
                        ${titleHtml}
                        <p><strong>Type:</strong> ${obstacle.type}</p>
                        <p><strong>Status:</strong> <span style="color: ${markerColor};">${statusText}</span></p>
                        <a href="${window.mapViewConfig.viewObstacleUrl}?id=${obstacle.id}">View Details</a>
                    </div>
                `;

                polygon.bindPopup(popupContent);
            }
        } catch (error) {
            console.error(`❌ Feil ved tegning av obstacle ${obstacle.id}:`, error);
        }
    });

    console.log(`✅ ${obstacles.length} markers tegnet!`);
}

function setupInitialEventListeners() {
    const searchInput = document.getElementById('search-input');
    searchInput.addEventListener('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(applyFilters, 300);
    });

    document.getElementById('filter-type-all').addEventListener('change', function () {
        if (this.checked) {
            document.querySelectorAll('.type-checkbox').forEach(cb => cb.checked = false);
        }
        applyFilters();
    });

    document.getElementById('filter-status-approved').addEventListener('change', applyFilters);
    document.getElementById('filter-status-pending').addEventListener('change', applyFilters);
    document.getElementById('filter-status-rejected').addEventListener('change', applyFilters);

    // Reset filters button
    const resetBtn = document.getElementById('reset-filters-btn');
    if (resetBtn) {
        resetBtn.addEventListener('click', resetFilters);
    }
}

function handleTypeFilterChange(event) {
    if (event.target.checked) {
        document.getElementById('filter-type-all').checked = false;
    }

    const anyTypeChecked = document.querySelectorAll('.type-checkbox:checked').length > 0;
    if (!anyTypeChecked) {
        document.getElementById('filter-type-all').checked = true;
    }

    applyFilters();
}

function applyFilters() {
    const searchText = document.getElementById('search-input').value.toLowerCase();
    const allTypesChecked = document.getElementById('filter-type-all').checked;

    const selectedTypes = [];
    if (!allTypesChecked) {
        document.querySelectorAll('.type-checkbox:checked').forEach(cb => {
            selectedTypes.push(cb.dataset.type);
        });
    }

    const showApproved = document.getElementById('filter-status-approved').checked;
    const showPending = document.getElementById('filter-status-pending').checked;
    const showRejected = document.getElementById('filter-status-rejected').checked;

    const filteredObstacles = allObstacles.filter(obstacle => {
        // ENDRET: Søk på ID eller Type i stedet for Navn
        if (searchText) {
            const idMatch = obstacle.id.toString().includes(searchText);
            const typeMatch = obstacle.type.toLowerCase().includes(searchText);
            if (!idMatch && !typeMatch) return false;
        }

        if (!allTypesChecked && selectedTypes.length > 0 && !selectedTypes.includes(obstacle.type)) return false;

        let matchesStatus = false;
        if (obstacle.isApproved && showApproved) matchesStatus = true;
        if (obstacle.isPending && showPending) matchesStatus = true;
        if (obstacle.isRejected && showRejected) matchesStatus = true;
        if (!matchesStatus) return false;

        return true;
    });

    console.log(`🔍 Viser ${filteredObstacles.length} av ${allObstacles.length}`);
    displayObstacles(filteredObstacles);
}

function resetFilters() {
    document.getElementById('search-input').value = '';
    document.getElementById('filter-type-all').checked = true;
    document.querySelectorAll('.type-checkbox').forEach(cb => cb.checked = false);
    document.getElementById('filter-status-approved').checked = true;
    document.getElementById('filter-status-pending').checked = true;
    document.getElementById('filter-status-rejected').checked = true;
    displayObstacles(allObstacles);
}