/**
 * Expo Demo Task Tracker
 * Tracks user progress through pilot test tasks
 */

class ExpoTaskTracker {
    constructor() {
        this.storageKey = 'expoTaskProgress';
        this.tasks = [
            {
                id: 'register',
                title: 'Opprett ny brukerkonto',
                description: 'Klikk "Sign Up" og registrer deg som ny bruker',
                completed: false
            },
            {
                id: 'login',
                title: 'Logg inn',
                description: 'Logg inn med din nye brukerkonto',
                completed: false
            },
            // FIKS 1: Fjernet "Se RegisterType" (Oppgave 3) herfra
            {
                id: 'create-quick-register',
                title: 'Opprett Quick Register',
                description: 'Lag en ny Quick Register oppføring',
                completed: false
            },
            {
                id: 'complete-quick-register',
                title: 'Fullfør Quick Register',
                description: 'Gå til My Registrations og fullfør Quick Register',
                completed: false
            },
            {
                id: 'create-full-register',
                title: 'Opprett Full Register',
                description: 'Lag en ny Full Register fra hjemmesiden',
                completed: false
            },
            {
                id: 'view-full-register',
                title: 'Sjekk Full Register',
                description: 'Klikk deg inn på det nye hinderet for å se detaljene', // Oppdatert beskrivelse
                completed: false
            }
        ];

        this.loadProgress();
        this.initEventListeners();

        // Check if all tasks are already completed
        this.checkCompletion();
    }

    /**
     * Load progress from localStorage
     */
    loadProgress() {
        try {
            const saved = localStorage.getItem(this.storageKey);
            if (saved) {
                const savedTasks = JSON.parse(saved);
                this.tasks = this.tasks.map(task => {
                    const savedTask = savedTasks.find(t => t.id === task.id);
                    return savedTask ? { ...task, completed: savedTask.completed } : task;
                });
            }
        } catch (error) {
            console.error('Failed to load task progress:', error);
        }
    }

    /**
     * Save progress to localStorage
     */
    saveProgress() {
        try {
            localStorage.setItem(this.storageKey, JSON.stringify(this.tasks));
        } catch (error) {
            console.error('Failed to save task progress:', error);
        }
    }

    /**
     * Mark a task as completed
     */
    completeTask(taskId) {
        const task = this.tasks.find(t => t.id === taskId);
        if (!task || task.completed) return;

        // Enforce sequential completion
        const taskIndex = this.tasks.findIndex(t => t.id === taskId);
        for (let i = 0; i < taskIndex; i++) {
            if (!this.tasks[i].completed) {
                // Tillater å fullføre login selv om register ikke er markert (hvis man logger rett inn)
                if (taskId === 'login' && this.tasks[i].id === 'register') continue;

                console.log(`Cannot complete task "${taskId}" - previous task "${this.tasks[i].id}" must be completed first`);
                return;
            }
        }

        task.completed = true;
        this.saveProgress();
        this.updateUI();
        this.checkCompletion();

        window.dispatchEvent(new CustomEvent('expoTaskCompleted', {
            detail: { taskId, task }
        }));
    }

    /**
     * Check if all tasks are completed
     */
    checkCompletion() {
        const allCompleted = this.tasks.every(task => task.completed);
        const currentPath = window.location.pathname.toLowerCase();
        const isOnCompletionPage = currentPath.includes('/pilot/expocompletion');

        if (allCompleted && !isOnCompletionPage) {
            setTimeout(() => {
                window.location.href = '/Pilot/ExpoCompletion';
            }, 1500);
        }
    }

    /**
     * Get completion percentage
     */
    getProgress() {
        const completed = this.tasks.filter(t => t.completed).length;
        const total = this.tasks.length;
        return total === 0 ? 0 : Math.round((completed / total) * 100);
    }

    /**
     * Update UI with current progress
     */
    updateUI() {
        const taskList = document.getElementById('expo-task-list');
        const progressBar = document.getElementById('expo-progress-bar');
        const progressText = document.getElementById('expo-progress-text');

        if (!taskList) return;

        taskList.innerHTML = this.tasks.map((task, index) => `
            <div class="expo-task-item ${task.completed ? 'completed' : ''}" data-task-id="${task.id}">
                <div class="expo-task-number">${index + 1}</div>
                <div class="expo-task-content">
                    <div class="expo-task-title">${task.title}</div>
                    <div class="expo-task-description">${task.description}</div>
                </div>
                <div class="expo-task-check">
                    ${task.completed ? '<svg class="checkmark" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>' : '<div class="unchecked-circle"></div>'}
                </div>
            </div>
        `).join('');

        const progress = this.getProgress();
        if (progressBar) progressBar.style.width = `${progress}%`;
        if (progressText) progressText.textContent = `${progress}% fullført`;
    }

    /**
     * Initialize event listeners
     */
    initEventListeners() {
        this.detectCurrentPage();

        document.addEventListener('submit', (e) => {
            this.handleFormSubmit(e);
        });

        window.addEventListener('popstate', () => {
            this.detectCurrentPage();
        });
    }

    /**
     * Detect current page and mark tasks as completed
     */
    detectCurrentPage() {
        const path = window.location.pathname.toLowerCase();
        const urlParams = new URLSearchParams(window.location.search);

        // Login check
        const isLoggedIn = document.querySelector('[data-user-authenticated="true"]');
        if (isLoggedIn) {
            this.completeTask('login');
        }

        // Register Type page
        if (path.includes('/pilot/registertype')) {
            if (urlParams.get('newUser') === 'true') {
                this.completeTask('register');
                this.completeTask('login');
            }
            if (urlParams.get('fullRegisterSuccess') === 'true') {
                this.completeTask('create-full-register');
            }
        }

        // My Registrations page
        if (path.includes('/pilot/myregistrations')) {
            // FIKS 2: Kun fullfør hvis vi har fått suksess-parameter fra CompleteQuickRegister-skjemaet
            if (urlParams.get('quickRegisterCompleted') === 'true') {
                this.completeTask('complete-quick-register');
            }
            // Vi har fjernet detectCompletedObstacles() kall herfra for å unngå "falsk" fullføring
        }

        // Complete Quick Register page
        if (path.includes('/pilot/completequickregister')) {
            this.completeTask('create-quick-register');
        }

        // FIKS 3: Sjekk om brukeren er inne på selve hinderet (Overview/Details)
        // Dette sikrer at oppgave 7 kun fullføres når de faktisk ser på hinderet
        if (path.includes('/pilot/overview') || path.includes('/pilot/viewobstacle')) {
            // Sjekk at vi faktisk har opprettet hinderet først
            const hasCreatedRegister = this.tasks.find(t => t.id === 'create-full-register')?.completed;
            if (hasCreatedRegister) {
                this.completeTask('view-full-register');
            }
        }
    }

    /**
     * Handle form submissions
     */
    handleFormSubmit(event) {
        const form = event.target;
        const action = form.action?.toLowerCase() || '';

        if (action.includes('/account/register')) {
            setTimeout(() => {
                if (!document.querySelector('.text-red-500')) {
                    this.completeTask('register');
                }
            }, 500);
        }

        // Her kan vi legge til mer spesifikk håndtering hvis nødvendig,
        // men URL-parameter sjekk i detectCurrentPage er ofte tryggere.
    }

    /**
     * Reset progress
     */
    reset() {
        localStorage.removeItem(this.storageKey);
        this.tasks.forEach(task => task.completed = false);
        this.updateUI();
    }

    /**
     * Toggle tracker visibility
     */
    toggleTracker() {
        const tracker = document.getElementById('expo-task-tracker');
        if (tracker) {
            tracker.classList.toggle('collapsed');
        }
    }
}

// Initialize tracker when DOM is ready
let expoTracker;
document.addEventListener('DOMContentLoaded', () => {
    expoTracker = new ExpoTaskTracker();
    expoTracker.updateUI();
    window.expoTracker = expoTracker;
});