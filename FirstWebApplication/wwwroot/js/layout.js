// Layout-specific JavaScript for navigation and dark mode

// Mobile menu toggle
function toggleMobileMenu() {
    const menu = document.getElementById('mobile-menu');
    menu.classList.toggle('hidden'); // Bruker 'hidden' klasse for å gjemme/vise
}

// Close mobile menu when clicking on links or outside
document.addEventListener('DOMContentLoaded', function () {
    // Attach event listeners to buttons (CSP-compliant)
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    if (mobileMenuButton) {
        mobileMenuButton.addEventListener('click', toggleMobileMenu);
    }

    // Close mobile menu when clicking outside
    document.addEventListener('click', function (event) {
        const menu = document.getElementById('mobile-menu');
        const nav = document.querySelector('nav');
        const isMenuOpen = menu && !menu.classList.contains('hidden');

        // Only close if menu is open and click is outside the entire nav area
        if (isMenuOpen && !nav.contains(event.target)) {
            menu.classList.add('hidden');
        }
    });

    // Close menu when clicking a link inside it
    const menuLinks = document.querySelectorAll('#mobile-menu a, #mobile-menu button[type="submit"]');
    menuLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            const menu = document.getElementById('mobile-menu');
            if (menu) {
                menu.classList.add('hidden');
            }
        });
    });
});