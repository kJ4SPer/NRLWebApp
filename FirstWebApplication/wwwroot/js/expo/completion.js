/**
 * Expo Completion Page Scripts
 */

document.addEventListener('DOMContentLoaded', function () {
    // --- FIKS START ---
    // Koble "Start på nytt"-knappen til funksjonen (fikser CSP/Scope problemer)
    const restartBtn = document.getElementById('btn-restart-expo');
    if (restartBtn) {
        restartBtn.addEventListener('click', resetExpoTest);
    }
    // --- FIKS SLUTT ---

    // Initialize QR code (if you add a QR library later)
    // For now, we'll just show the placeholder

    // Optionally, you can add confetti or celebration animations here
    celebrateCompletion();

    // Update feedback URL if you have one
    const feedbackUrl = 'https://forms.google.com/your-form-link'; // Replace with actual URL
    const feedbackLink = document.getElementById('feedback-url');
    if (feedbackLink) {
        feedbackLink.href = feedbackUrl;
        feedbackLink.textContent = 'forms.google.com';
    }

    // Generate QR code if QRCode library is available
    generateQRCode(feedbackUrl);
});

/**
 * Celebration animation
 */
function celebrateCompletion() {
    // Add a simple scale animation to the success icon
    const successIcon = document.querySelector('.success-icon');
    if (successIcon) {
        setTimeout(() => {
            successIcon.style.animation = 'iconPulse 1.5s ease infinite, iconPop 0.5s ease';
        }, 300);
    }
}

/**
 * Reset expo test
 */
function resetExpoTest() {
    const confirmed = confirm('Er du sikker på at du vil starte testen på nytt? Dette vil nullstille all fremgang og logge deg ut.');

    if (confirmed) {
        // 1. Slett local storage data
        localStorage.removeItem('expoTaskProgress');
        localStorage.removeItem('expoStatus');

        // 2. Finn utloggings-skjemaet i menyen og send det
        // Dette sikrer at vi logger ut korrekt med riktig sikkerhetstoken
        const logoutForm = document.querySelector('form[action*="/Account/Logout"]');

        if (logoutForm) {
            logoutForm.submit();
        } else {
            // Fallback: Hvis skjemaet ikke finnes, send til forsiden
            console.error("Kunne ikke finne utloggingsskjema");
            window.location.href = '/';
        }
    }
}

/**
 * Generate QR Code
 */
function generateQRCode(url) {
    if (typeof QRCode !== 'undefined') {
        const qrContainer = document.getElementById('qr-code-image');
        if (qrContainer) {
            qrContainer.innerHTML = '';
            new QRCode(qrContainer, {
                text: url,
                width: 180,
                height: 180,
                colorDark: '#667eea',
                colorLight: '#ffffff',
                correctLevel: QRCode.CorrectLevel.H
            });
        }
    } else {
        console.info('QRCode library not loaded. Showing placeholder.');
    }
}

/**
 * Share results (optional feature)
 */
function shareResults() {
    const shareData = {
        title: 'Expo Test Completed!',
        text: 'I just completed the Obstacle Registration System Expo test! 🎉',
        url: window.location.href
    };

    if (navigator.share) {
        navigator.share(shareData)
            .then(() => console.log('Shared successfully'))
            .catch((error) => console.log('Error sharing:', error));
    } else {
        console.log('Web Share API not supported');
    }
}

/**
 * Print certificate (optional feature)
 */
function printCertificate() {
    window.print();
}

// Additional animation keyframe (if needed)
const style = document.createElement('style');
style.textContent = `
    @keyframes iconPop {
        0% {
            transform: scale(1);
        }
        50% {
            transform: scale(1.2);
        }
        100% {
            transform: scale(1);
        }
    }
`;
document.head.appendChild(style);