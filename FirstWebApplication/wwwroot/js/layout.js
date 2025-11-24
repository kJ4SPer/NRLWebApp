// Layout-specific JavaScript for navigation and dark mode

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

    const darkModeToggle = document.getElementById('dark-mode-toggle');
    if (darkModeToggle) {
        darkModeToggle.addEventListener('click', toggleDarkMode);
    }

    const mobileDarkModeToggle = document.getElementById('mobile-dark-mode-toggle');
    if (mobileDarkModeToggle) {
        mobileDarkModeToggle.addEventListener('click', toggleDarkMode);
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

    // Initialize dark mode on page load
    initializeDarkMode();
});

// Dark mode functionality
function toggleDarkMode() {
    const body = document.body;
    const isDark = body.classList.toggle('dark');

    // Update icons
    updateDarkModeIcons(isDark);

    // Save preference
    localStorage.setItem('darkMode', isDark ? 'enabled' : 'disabled');
}

function updateDarkModeIcons(isDark) {
    const sunIcon = document.getElementById('sun-icon');
    const moonIcon = document.getElementById('moon-icon');
    const mobileSunIcon = document.getElementById('mobile-sun-icon');
    const mobileMoonIcon = document.getElementById('mobile-moon-icon');
    const darkModeText = document.getElementById('dark-mode-text');

    if (isDark) {
        if (sunIcon) sunIcon.classList.remove('hidden');
        if (moonIcon) moonIcon.classList.add('hidden');
        if (mobileSunIcon) mobileSunIcon.classList.remove('hidden');
        if (mobileMoonIcon) mobileMoonIcon.classList.add('hidden');
        if (darkModeText) darkModeText.textContent = 'Light Mode';
    } else {
        if (sunIcon) sunIcon.classList.add('hidden');
        if (moonIcon) moonIcon.classList.remove('hidden');
        if (mobileSunIcon) mobileSunIcon.classList.add('hidden');
        if (mobileMoonIcon) mobileMoonIcon.classList.remove('hidden');
        if (darkModeText) darkModeText.textContent = 'Dark Mode';
    }
}

function initializeDarkMode() {
    const darkMode = localStorage.getItem('darkMode');

    if (darkMode === 'enabled') {
        document.body.classList.add('dark');
        updateDarkModeIcons(true);
    } else if (darkMode === null) {
        // Check system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.body.classList.add('dark');
            updateDarkModeIcons(true);
        }
    }
}
