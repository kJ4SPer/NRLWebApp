// Layout-specific JavaScript for navigation

// Mobile menu toggle
function toggleMobileMenu() {
    const menu = document.getElementById('mobile-menu');
    menu.classList.toggle('active');
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
        const button = document.querySelector('.mobile-menu-button');
        const nav = document.querySelector('nav');

        // Only close if click is outside the entire nav area
        if (menu && menu.classList.contains('active') && !nav.contains(event.target)) {
            menu.classList.remove('active');
        }
    });

    // Close menu when clicking a link inside it
    const menuLinks = document.querySelectorAll('#mobile-menu a, #mobile-menu button[type="submit"]');
    menuLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            const menu = document.getElementById('mobile-menu');
            if (menu) {
                menu.classList.remove('active');
            }
        });
    });
});
