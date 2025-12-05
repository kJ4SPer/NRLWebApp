var map;
var userMarker;
var currentPosition = null;

document.addEventListener('DOMContentLoaded', function () {
    map = L.map('quick-register-map', {
        center: [60.4720, 8.4689],
        zoom: 6,
        zoomControl: false
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    getUserLocation();
});

function getUserLocation() {
    console.log('Attempting to get user location...');

    if (!("geolocation" in navigator)) {
        console.error('Geolocation not supported');
        showError("Your browser doesn't support location services. Please use Full Register instead.");
        return;
    }

    const isSecureContext = window.isSecureContext || window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1';

    if (!isSecureContext) {
        console.warn('Not running in secure context (HTTPS or localhost)');
        showError("Location access requires HTTPS or localhost. Please use Full Register or access via localhost.");
        return;
    }

    navigator.geolocation.getCurrentPosition(
        handleLocationSuccess,
        handleLocationError,
        {
            enableHighAccuracy: true,
            timeout: 15000,
            maximumAge: 0
        }
    );
}

function handleLocationSuccess(position) {
    console.log('Location success:', position);

    var lat = position.coords.latitude;
    var lng = position.coords.longitude;

    currentPosition = { lat: lat, lng: lng };

    map.setView([lat, lng], 15);

    userMarker = L.marker([lat, lng], {
        icon: L.icon({
            iconUrl: '/lib/leaflet/images/marker-icon.png',
            shadowUrl: '/lib/leaflet/images/marker-shadow.png',
            iconSize: [25, 41],
            iconAnchor: [12, 41],
            popupAnchor: [1, -34],
            shadowSize: [41, 41]
        })
    }).addTo(map);

   
    proceedWithRegistration();
}

function handleLocationError(error) {
    console.error('Location error:', error);

    let errorMessage = "We couldn't get your location. ";

    switch(error.code) {
        case error.PERMISSION_DENIED:
            errorMessage += "You denied location access. Please allow location in your browser settings.";
            break;
        case error.POSITION_UNAVAILABLE:
            errorMessage += "Location information is unavailable. Check your device settings.";
            break;
        case error.TIMEOUT:
            errorMessage += "Location request timed out. Please try again.";
            break;
        default:
            errorMessage += "An unknown error occurred.";
    }

    if (!window.isSecureContext && window.location.hostname !== 'localhost') {
        errorMessage += " Note: Location requires HTTPS or localhost.";
    }

    showError(errorMessage);
}

function showError(message) {
    document.getElementById('loading-state').classList.add('hidden');
    document.getElementById('error-state').classList.remove('hidden');
    document.getElementById('error-message').textContent = message;
}

function proceedWithRegistration() {
    if (!currentPosition) {
        console.error('No position available');
        return;
    }

    const wkt = `POINT(${currentPosition.lng} ${currentPosition.lat})`;
    console.log('Saving registration:', wkt);

    const quickRegisterUrl = document.getElementById('quick-register-map').getAttribute('data-quick-register-url');

    fetch(quickRegisterUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `obstacleGeometry=${encodeURIComponent(wkt)}`
    })
        .then(response => {
            if (response.ok) {
                showSuccess();
            } else {
                throw new Error('Save failed');
            }
        })
        .catch(error => {
            console.error('Error saving:', error);
            showError('Failed to save registration. Please try again.');
        });
}

function showSuccess() {
    document.getElementById('loading-state').classList.add('hidden');
    document.getElementById('success-state').classList.remove('hidden');

    let seconds = 3;
    const countdownElement = document.getElementById('countdown');
    const registerTypeUrl = document.getElementById('quick-register-map').getAttribute('data-register-type-url');

    const timer = setInterval(function() {
        seconds--;
        countdownElement.textContent = seconds;

        if (seconds <= 0) {
            clearInterval(timer);
            window.location.href = registerTypeUrl;
        }
    }, 1000);
}

function retryLocation() {
    document.getElementById('error-state').classList.add('hidden');
    document.getElementById('loading-state').classList.remove('hidden');
    getUserLocation();
}
