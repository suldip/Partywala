// City to Pin Code Mapping Data
const cityPinCodeData = {
    "Mumbai": "400001", "Pune": "411001", "Nagpur": "440001", "Nashik": "422001", "Aurangabad": "431001", "Thane": "400601", "Solapur": "413001",
    "New Delhi": "110001", "Central Delhi": "110002", "North Delhi": "110007", "South Delhi": "110017", "East Delhi": "110031", "West Delhi": "110015",
    "Bangalore": "560001", "Mysore": "570001", "Mangalore": "575001", "Hubli": "580020", "Belgaum": "590001", "Gulbarga": "585101",
    "Chennai": "600001", "Coimbatore": "641001", "Madurai": "625001", "Tiruchirappalli": "620001", "Salem": "636001", "Tirunelveli": "627001",
    "Ahmedabad": "380001", "Surat": "395001", "Vadodara": "390001", "Rajkot": "360001", "Bhavnagar": "364001", "Jamnagar": "361001",
    "Jaipur": "302001", "Jodhpur": "342001", "Udaipur": "313001", "Kota": "324001", "Ajmer": "305001", "Bikaner": "334001",
    "Lucknow": "226001", "Kanpur": "208001", "Ghaziabad": "201001", "Agra": "282001", "Varanasi": "221001", "Meerut": "250001",
    "Kolkata": "700001", "Howrah": "711101", "Durgapur": "713201", "Asansol": "713301", "Siliguri": "734001",
    "Hyderabad": "500001", "Warangal": "506001", "Nizamabad": "503001", "Karimnagar": "505001", "Khammam": "507001",
    "Visakhapatnam": "530001", "Vijayawada": "520001", "Guntur": "522001", "Nellore": "524001", "Kurnool": "518001",
    "Thiruvananthapuram": "695001", "Kochi": "682001", "Kozhikode": "673001", "Thrissur": "680001", "Kollam": "691001",
    "Bhopal": "462001", "Indore": "452001", "Gwalior": "474001", "Jabalpur": "482001", "Ujjain": "456001",
    "Chandigarh": "160001", "Ludhiana": "141001", "Amritsar": "143001", "Jalandhar": "144001", "Patiala": "147001",
    "Gurugram": "122001", "Faridabad": "121001", "Panipat": "132103", "Ambala": "133001", "Karnal": "132001",
    "Patna": "800001", "Gaya": "823001", "Bhagalpur": "812001", "Muzaffarpur": "842001", "Darbhanga": "846004"
};

// Service Categories Data
const SERVICE_CATEGORIES = [
    {
        id: 'venue',
        name: 'Venue / Banquet',
        icon: '🏰',
        description: 'Banquet halls, lawns, farmhouses for events',
        subCategories: [
            { id: 'banquet_hall', name: 'Banquet Hall', description: 'Indoor hall for weddings and parties', tags: ['AC', 'Indoor'] },
            { id: 'lawn', name: 'Party Lawn', description: 'Open air lawn for large gatherings', tags: ['Outdoor', 'Spacious'] },
            { id: 'farmhouse', name: 'Farmhouse', description: 'Private property for pool parties/stays', tags: ['Private', 'Pool'] },
            { id: 'resort', name: 'Resort / Hotel', description: 'Luxury stay and event space', tags: ['Luxury', 'Stay'] }
        ],
        customFields: [
            { id: 'capacity', label: 'Guest Capacity', type: 'number', placeholder: 'e.g., 500' },
            { id: 'rooms', label: 'Rooms Available', type: 'number', placeholder: 'e.g., 10' },
            { id: 'parking', label: 'Parking Capacity', type: 'number', placeholder: 'e.g., 50 cars' }
        ],
        pricingModels: [
            { id: 'day', name: 'Per Day', unit: 'day', description: 'Rent per day' },
            { id: 'plate', name: 'Per Plate', unit: 'plate', description: 'Charge per plate (includes venue)' }
        ]
    },
    {
        id: 'makeup',
        name: 'Makeup Artist',
        icon: '💄',
        description: 'Bridal makeup, party makeup, hairstyling',
        subCategories: [
            { id: 'bridal', name: 'Bridal Makeup', description: 'Complete bridal makeover', tags: ['HD', 'Airbrush'] },
            { id: 'party', name: 'Party Makeup', description: 'Light makeup for guests', tags: ['Subtle', 'Glam'] },
            { id: 'hairstyling', name: 'Hairstyling', description: 'Creative hairdos', tags: ['Trendy', 'Traditional'] }
        ],
        customFields: [
            { id: 'brands', label: 'Brands Used', type: 'multiselect', options: ['MAC', 'Huda Beauty', 'Bobbi Brown', 'Kryolan', 'Sephora'] },
            { id: 'travel', label: 'Travels to Venue?', type: 'multiselect', options: ['Yes', 'No', 'Extra Charge'] }
        ],
        pricingModels: [
            { id: 'package', name: 'Per Package', unit: 'package', description: 'Fixed price for makeup package' },
            { id: 'person', name: 'Per Person', unit: 'person', description: 'Charge per person' }
        ]
    },
    {
        id: 'photographer',
        name: 'Photographer',
        icon: '📸',
        description: 'Wedding, pre-wedding, and event photography',
        subCategories: [
            { id: 'wedding', name: 'Wedding Photography', description: 'Traditional and candid shots', tags: ['Candid', 'Traditional'] },
            { id: 'pre_wedding', name: 'Pre-Wedding Shoot', description: 'Couple shoot before wedding', tags: ['Creative', 'Outdoor'] },
            { id: 'cinematography', name: 'Cinematography', description: 'Wedding films and teasers', tags: ['Video', 'Cinematic'] },
            { id: 'drone', name: 'Drone Coverage', description: 'Aerial shots of the event', tags: ['Aerial', '4K'] }
        ],
        customFields: [
            { id: 'deliverables', label: 'Deliverables', type: 'multiselect', options: ['Raw Data', 'Edited Photos', 'Album', 'Teaser', 'Full Film'] },
            { id: 'delivery_time', label: 'Delivery Time', type: 'text', placeholder: 'e.g., 4 weeks' }
        ],
        pricingModels: [
            { id: 'day', name: 'Per Day', unit: 'day', description: 'Charge per day of coverage' },
            { id: 'package', name: 'Full Package', unit: 'package', description: 'Fixed price for entire event' }
        ]
    },
    {
        id: 'decorator',
        name: 'Decorator',
        icon: '🎨',
        description: 'Stage, mandap, floral, and theme decoration',
        subCategories: [
            { id: 'wedding_decor', name: 'Wedding Decor', description: 'Mandap, stage, entrance', tags: ['Grand', 'Traditional'] },
            { id: 'floral', name: 'Floral Decor', description: 'Fresh flower arrangements', tags: ['Aromatic', 'Natural'] },
            { id: 'balloon', name: 'Balloon Decor', description: 'For birthdays and parties', tags: ['Colorful', 'Fun'] },
            { id: 'lighting', name: 'Lighting Decor', description: 'Ambiance lighting and effects', tags: ['Bright', 'Mood'] }
        ],
        customFields: [
            { id: 'specialization', label: 'Specialization', type: 'text', placeholder: 'e.g., Vintage Themes, Royal Weddings' }
        ],
        pricingModels: [
            { id: 'package', name: 'Per Package', unit: 'package', description: 'Fixed price for decoration setup' }
        ]
    },
    {
        id: 'caterer',
        name: 'Caterer / Food',
        icon: '🍽️',
        description: 'Buffet, sit-down dinners, food stalls',
        subCategories: [
            { id: 'wedding_catering', name: 'Wedding Catering', description: 'Large scale buffet service', tags: ['Buffet', 'Live Counters'] },
            { id: 'snacks', name: 'Snacks & Chaat', description: 'Small bites and street food', tags: ['Spicy', 'Quick'] },
            { id: 'dessert', name: 'Dessert Counter', description: 'Sweets, cakes, ice creams', tags: ['Sweet', 'Variety'] },
            { id: 'bartender', name: 'Bartending Services', description: 'Drinks and cocktails', tags: ['Cocktails', 'Mocktails'] }
        ],
        customFields: [
            { id: 'cuisines', label: 'Cuisines Offered', type: 'multiselect', options: ['North Indian', 'South Indian', 'Chinese', 'Italian', 'Continental', 'Mexican'] },
            { id: 'min_capacity', label: 'Min Capacity', type: 'number', placeholder: 'e.g., 50' }
        ],
        pricingModels: [
            { id: 'plate', name: 'Per Plate', unit: 'plate', description: 'Charge per plate' }
        ]
    },
    {
        id: 'mehendi',
        name: 'Mehendi Artist',
        icon: '🖐️',
        description: 'Bridal and guest mehendi designs',
        subCategories: [
            { id: 'bridal_mehendi', name: 'Bridal Mehendi', description: 'Intricate full hands and legs', tags: ['Intricate', 'Traditional'] },
            { id: 'guest_mehendi', name: 'Guest Mehendi', description: 'Simple designs for guests', tags: ['Quick', 'Simple'] },
            { id: 'arabic', name: 'Arabic Design', description: 'Flowing floral patterns', tags: ['Modern', 'Stylish'] }
        ],
        customFields: [
            { id: 'organic', label: 'Uses Organic Henna?', type: 'multiselect', options: ['Yes', 'No'] }
        ],
        pricingModels: [
            { id: 'hand', name: 'Per Hand', unit: 'hand', description: 'Charge per hand/side' },
            { id: 'package', name: 'Bridal Package', unit: 'package', description: 'Fixed price for bride' }
        ]
    },
    {
        id: 'dj',
        name: 'DJ / Sound',
        icon: '🎧',
        description: 'DJ, sound system, dance floor',
        subCategories: [
            { id: 'wedding_dj', name: 'Wedding DJ', description: 'Sangeet and reception music', tags: ['Bollywood', 'Punjabi'] },
            { id: 'party_dj', name: 'Party DJ', description: 'Club style music for parties', tags: ['EDM', 'Hip Hop'] },
            { id: 'sound_setup', name: 'Sound Setup', description: 'Speakers, mics, console', tags: ['High Quality', 'Loud'] }
        ],
        customFields: [
            { id: 'genres', label: 'Genres', type: 'multiselect', options: ['Bollywood', 'Punjabi', 'EDM', 'House', 'Retro'] }
        ],
        pricingModels: [
            { id: 'event', name: 'Per Event', unit: 'event', description: 'Charge per event' },
            { id: 'hour', name: 'Per Hour', unit: 'hour', description: 'Charge per hour' }
        ]
    },
    {
        id: 'entertainment',
        name: 'Entertainment',
        icon: '🎤',
        description: 'Singers, dancers, anchors, magicians',
        subCategories: [
            { id: 'singer', name: 'Live Singer', description: 'Solo or band performance', tags: ['Melodious', 'Live'] },
            { id: 'anchor', name: 'Anchor / Emcee', description: 'Host for the event', tags: ['Engaging', 'Fun'] },
            { id: 'dancer', name: 'Dance Troupe', description: 'Group dance performance', tags: ['Energetic', 'Choreographed'] },
            { id: 'magician', name: 'Magician', description: 'Magic show for kids/adults', tags: ['Mystery', 'Fun'] }
        ],
        customFields: [
            { id: 'duration', label: 'Performance Duration', type: 'text', placeholder: 'e.g., 60 mins' }
        ],
        pricingModels: [
            { id: 'show', name: 'Per Show', unit: 'show', description: 'Charge per performance' }
        ]
    },
    {
        id: 'planner',
        name: 'Event Planner',
        icon: '📋',
        description: 'Complete event management and planning',
        subCategories: [
            { id: 'wedding_planner', name: 'Wedding Planner', description: 'End-to-end wedding planning', tags: ['Full Service', 'Coordination'] },
            { id: 'birthday_planner', name: 'Birthday Planner', description: 'Kids or adult birthday planning', tags: ['Themed', 'Fun'] },
            { id: 'corporate', name: 'Corporate Events', description: 'Conferences and parties', tags: ['Professional', 'Formal'] }
        ],
        customFields: [
            { id: 'team_size', label: 'Team Size', type: 'number', placeholder: 'e.g., 5' }
        ],
        pricingModels: [
            { id: 'fee', name: 'Fixed Fee', unit: 'event', description: 'Management fee for the event' }
        ]
    },
    {
        id: 'pandit',
        name: 'Pandit / Priest',
        icon: '🔥',
        description: 'For wedding rituals and poojas',
        subCategories: [
            { id: 'wedding_pandit', name: 'Wedding Pandit', description: 'For pheras and marriage rituals', tags: ['Vedic', 'Traditional'] },
            { id: 'pooja', name: 'Pooja / Havan', description: 'Griha pravesh, satyanarayan, etc.', tags: ['Rituals', 'Blessings'] }
        ],
        customFields: [
            { id: 'languages', label: 'Languages Known', type: 'multiselect', options: ['Hindi', 'Sanskrit', 'English', 'Marathi', 'Gujarati'] }
        ],
        pricingModels: [
            { id: 'dakshina', name: 'Dakshina', unit: 'event', description: 'Fixed amount for the ritual' }
        ]
    }
];

// State-City Cascading Dropdown Data - Loaded from database
let stateCityData = {};
let allStates = [];
let cityPinCodeMap = {}; // Map city to pin codes

// Wizard State
let currentStep = 1;
const totalSteps = 5;
const stepData = [
    { title: 'Personal Information', description: 'Provide your contact details and address information', stepDesc: '👤 Enter your personal details and address information' },
    { title: 'Service Category', description: 'Choose your main service category and specializations', stepDesc: '🎯 Select your service category and specializations' },
    { title: 'Portfolio & Showcase', description: 'Upload your work samples to attract customers', stepDesc: '📁 Upload voice samples, photos, and showcase your work' },
    { title: 'Pricing & Payment', description: 'Set your rates and pricing model', stepDesc: '💰 Set your rates and pricing model' },
    { title: 'Payment Account Setup', description: 'Add your bank accounts and UPI details for receiving payments', stepDesc: '🏦 Add your bank accounts and UPI for receiving payments' }
];
let selectedCategory = null;
let selectedSubCategories = [];

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    // Initialize cascade and other features first (they will work even if DB load fails)
    initStateCityCascade();
    initLocationAdder();
    initCityPinCodeAutoPopulate();
    initWizard();
    renderCategories();

    // Load locations from database and repopulate state dropdowns
    loadLocationsFromDB().then(() => {
        // Re-initialize cascade after states are loaded (in case dropdowns were recreated)
        initStateCityCascade();
        console.log('Initialization complete');
    }).catch(error => {
        console.error('Error loading locations from DB:', error);
        // Still initialize cascade even if DB load fails
        initStateCityCascade();
    });

    // Prevent default form submission and use our custom submitForm
    const form = document.querySelector('#vendor-registration-form');
    const submitButton = document.getElementById('btn-submit');

    if (form) {
        form.addEventListener('submit', async function (e) {
            // Only prevent default if this is not a programmatic submit
            if (!form.dataset.programmaticSubmit) {
                e.preventDefault();
                // Validate step 5 before submitting
                if (await validateStep(5)) {
                    submitForm();
                }
            } else {
                // Clear the flag for next time
                delete form.dataset.programmaticSubmit;
            }
        });
    }

    if (submitButton) {
        submitButton.addEventListener('click', async function (e) {
            e.preventDefault();
            console.log('Submit button clicked');
            if (await validateStep(5)) {
                console.log('Validation passed, calling submitForm');
                submitForm();
            } else {
                console.log('Validation failed - check required fields');
            }
        });
    }
});

// Load locations from database
async function loadLocationsFromDB() {
    try {
        // Ensure basePath is set
        if (!window.basePath) {
            const basePathEl = document.querySelector('meta[name="base-path"]');
            if (basePathEl) {
                window.basePath = basePathEl.content;
            } else {
                // Fallback: detect from URL
                const path = window.location.pathname;
                window.basePath = path.startsWith('/PartyClap') ? '/PartyClap/' : '/';
            }
        }
        if (!window.basePath.endsWith('/')) {
            window.basePath += '/';
        }

        // Load all states
        const statesUrl = window.basePath + 'Vendor/GetStates';
        console.log('Fetching states from:', statesUrl);
        const statesResponse = await fetch(statesUrl);

        if (!statesResponse.ok) {
            throw new Error(`HTTP ${statesResponse.status}: ${statesResponse.statusText}`);
        }

        allStates = await statesResponse.json();

        // Populate state dropdowns
        populateStateDropdowns();

        // Load cities for each state (lazy load when state is selected)
        console.log('Loaded', allStates.length, 'states from database');
    } catch (error) {
        console.error('Error loading locations:', error);
        // Fallback to empty arrays
        allStates = [];
    }
}

// Populate state dropdowns with data from database
function populateStateDropdowns() {
    const stateDropdowns = document.querySelectorAll('select[data-cascade="state"]');
    console.log(`Populating ${stateDropdowns.length} state dropdown(s) with ${allStates.length} states`);

    stateDropdowns.forEach(dropdown => {
        // Store the current value if any
        const currentValue = dropdown.value;

        // Clear existing options except the first one
        const firstOption = dropdown.querySelector('option[value=""]');
        dropdown.innerHTML = '';
        if (firstOption) {
            dropdown.appendChild(firstOption);
        } else {
            const defaultOption = document.createElement('option');
            defaultOption.value = '';
            defaultOption.textContent = 'Select your state';
            dropdown.appendChild(defaultOption);
        }

        // Add states from database
        if (Array.isArray(allStates)) {
            allStates.forEach(state => {
                if (state && typeof state === 'string') {
                    const option = document.createElement('option');
                    option.value = state;
                    option.textContent = state;
                    dropdown.appendChild(option);
                }
            });
        }

        // Restore previous value if it still exists
        if (currentValue && dropdown.querySelector(`option[value="${currentValue}"]`)) {
            dropdown.value = currentValue;
        }
    });

    // Re-initialize cascade after populating (in case event listeners were lost)
    setTimeout(() => {
        initStateCityCascade();
    }, 100);
}

// Load cities for a selected state
async function loadCitiesForState(state) {
    if (!state) {
        console.warn('loadCitiesForState called with empty state');
        return [];
    }

    // Trim the state name
    const trimmedState = state.trim();

    // Check if we already have cities for this state cached
    if (stateCityData[trimmedState]) {
        console.log('Using cached cities for state:', trimmedState);
        return stateCityData[trimmedState];
    }

    try {
        // Ensure basePath is set (with trailing slash)
        if (!window.basePath) {
            const basePathEl = document.querySelector('meta[name="base-path"]');
            window.basePath = basePathEl ? basePathEl.content : '/PartyClap/';
        }
        if (!window.basePath.endsWith('/')) {
            window.basePath += '/';
        }

        const url = window.basePath + `Vendor/GetCities?state=${encodeURIComponent(trimmedState)}`;
        console.log('Fetching cities from:', url);

        const response = await fetch(url);
        console.log('Response status:', response.status, response.statusText);

        if (!response.ok) {
            console.error(`Error loading cities: HTTP ${response.status} - ${response.statusText}`);
            const errorText = await response.text();
            console.error('Error response:', errorText);
            return [];
        }

        const cities = await response.json();
        console.log('Raw cities response:', cities);

        if (Array.isArray(cities)) {
            // Filter out empty/null values and trim
            const validCities = cities
                .filter(city => city && typeof city === 'string' && city.trim().length > 0)
                .map(city => city.trim());

            console.log(`Valid cities for ${trimmedState}:`, validCities.length, validCities);

            if (validCities.length > 0) {
                stateCityData[trimmedState] = validCities;

                // Also load pin codes for cities and cache them (async, don't wait)
                validCities.forEach(city => {
                    loadPinCodesForCity(city).catch(err =>
                        console.warn('Error loading pin codes for city:', city, err)
                    );
                });
            }

            return validCities;
        } else {
            console.error('Invalid response format for cities. Expected array, got:', typeof cities, cities);
            return [];
        }
    } catch (error) {
        console.error('Error loading cities for state:', trimmedState, error);
        console.error('Error stack:', error.stack);
        return [];
    }
}

// Load pin codes for a city
async function loadPinCodesForCity(city) {
    if (!city || cityPinCodeMap[city]) return;

    try {
        const response = await fetch(`${window.basePath || ''}Vendor/GetPinCodes?city=${encodeURIComponent(city)}`);
        if (!response.ok) {
            console.error(`Error loading pin codes: HTTP ${response.status} - ${response.statusText}`);
            return;
        }
        const locations = await response.json();

        if (locations && Array.isArray(locations) && locations.length > 0) {
            // Store the first pin code for this city (most common)
            cityPinCodeMap[city] = locations[0].PinCode;
        }
    } catch (error) {
        console.error('Error loading pin codes for city:', city, error);
    }
}

// Search pin codes
async function searchPinCodes(searchTerm) {
    if (!searchTerm || searchTerm.length < 3) return [];

    try {
        const response = await fetch(`${window.basePath || ''}Vendor/SearchPinCodes?searchTerm=${encodeURIComponent(searchTerm)}`);
        const locations = await response.json();
        return locations;
    } catch (error) {
        console.error('Error searching pin codes:', error);
        return [];
    }
}

// Wizard Functions
function initWizard() {
    updateStepVisibility();

    // Next Button
    document.getElementById('btn-next').addEventListener('click', async function () {
        if (await validateStep(currentStep)) {
            if (currentStep < totalSteps) {
                currentStep++;
                updateStepVisibility();
                window.scrollTo(0, 0);
            }
        }
    });

    // Previous Button
    document.getElementById('btn-prev').addEventListener('click', function () {
        if (currentStep > 1) {
            currentStep--;
            updateStepVisibility();
            window.scrollTo(0, 0);
        }
    });

    // Dynamic Pricing Toggle
    const dynamicPricingToggle = document.getElementById('enable-dynamic-pricing');
    const dynamicPricingContent = document.getElementById('dynamic-pricing-content');

    if (dynamicPricingToggle && dynamicPricingContent) {
        dynamicPricingToggle.addEventListener('change', function () {
            if (this.checked) {
                dynamicPricingContent.style.display = 'block';
            } else {
                dynamicPricingContent.style.display = 'none';
                // Clear dynamic pricing inputs when disabled
                document.getElementById('weekday-base').value = '';
                document.getElementById('weekday-discount').value = '';
                document.getElementById('weekend-base').value = '';
                document.getElementById('weekend-discount').value = '';
                document.getElementById('festival-base').value = '';
                document.getElementById('festival-discount').value = '';
                document.querySelectorAll('.festival-checkbox').forEach(cb => cb.checked = false);
            }
        });
    }
}

function updateStepVisibility() {
    // Hide all steps
    document.querySelectorAll('.step-content').forEach(el => el.classList.remove('active'));

    // Show current step
    document.getElementById(`step-${currentStep}`).classList.add('active');

    // Update Progress Indicator
    document.querySelectorAll('.wizard-step').forEach((el, index) => {
        const stepNum = index + 1;
        el.classList.remove('active', 'completed');
        if (stepNum === currentStep) {
            el.classList.add('active');
        } else if (stepNum < currentStep) {
            el.classList.add('completed');
        }
    });

    // Update Step Counter
    const stepNumEl = document.getElementById('current-step-num');
    if (stepNumEl) stepNumEl.textContent = currentStep;

    // Update Connector Fill
    const connectorFill = document.getElementById('wizard-connector-fill');
    if (connectorFill) {
        const progressPercentage = ((currentStep - 1) / (totalSteps - 1)) * 100;
        connectorFill.style.width = `${progressPercentage}%`;
    }

    // Update Texts
    const data = stepData[currentStep - 1];
    if (data) {
        const stepDescEl = document.getElementById('step-description');
        if (stepDescEl) stepDescEl.textContent = data.stepDesc;

        const cardTitleEl = document.getElementById('card-title');
        if (cardTitleEl) cardTitleEl.textContent = data.title;

        const cardDescEl = document.getElementById('card-description');
        if (cardDescEl) cardDescEl.textContent = data.description;
    }

    // Update Buttons
    const btnPrev = document.getElementById('btn-prev');
    const btnNext = document.getElementById('btn-next');
    const btnSubmit = document.getElementById('btn-submit');

    if (currentStep === 1) {
        btnPrev.style.display = 'none';
    } else {
        btnPrev.style.display = 'block';
    }

    if (currentStep === totalSteps) {
        btnNext.style.display = 'none';
        btnSubmit.style.display = 'block';
    } else {
        btnNext.style.display = 'block';
        btnSubmit.style.display = 'none';
    }

    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

async function validateStep(step) {
    if (step === 1) {
        const name = document.querySelector('input[name="Name"]');
        const email = document.querySelector('input[name="Email"]');
        const phone = document.querySelector('input[name="Phone"]');
        const pincode = document.querySelector('input[name="PinCode"]');

        let isValid = true;

        if (!name.value.trim()) {
            name.classList.add('is-invalid');
            isValid = false;
        } else {
            name.classList.remove('is-invalid');
        }

        if (!email.value.trim() || !email.value.includes('@')) {
            email.classList.add('is-invalid');
            isValid = false;
        } else {
            email.classList.remove('is-invalid');
        }

        if (!phone.value.trim() || phone.value.length < 10) {
            phone.classList.add('is-invalid');
            isValid = false;
        } else {
            phone.classList.remove('is-invalid');
        }

        if (!pincode.value.trim()) {
            pincode.classList.add('is-invalid');
            isValid = false;
        } else {
            pincode.classList.remove('is-invalid');
        }

        if (!isValid) {
            SmartPop.error('Validation Error', 'Please fill in all required fields correctly.');
            return false;
        }
    }

    if (step === 2 && !selectedCategory) {
        SmartPop.warning('Missing Category', 'Please select a service category to proceed.');
        return false;
    }

    if (step === 4) {
        const dynamicPricingEnabled = document.getElementById('enable-dynamic-pricing')?.checked;

        if (dynamicPricingEnabled) {
            const weekdayBase = document.getElementById('weekday-base');
            const weekendBase = document.getElementById('weekend-base');
            const festivalBase = document.getElementById('festival-base');

            let isValid = true;
            let errorMsg = 'Please enter base rates for: ';
            let missing = [];

            if (!weekdayBase.value) { weekdayBase.classList.add('is-invalid'); isValid = false; missing.push('Weekday'); } else weekdayBase.classList.remove('is-invalid');
            if (!weekendBase.value) { weekendBase.classList.add('is-invalid'); isValid = false; missing.push('Weekend'); } else weekendBase.classList.remove('is-invalid');
            if (!festivalBase.value) { festivalBase.classList.add('is-invalid'); isValid = false; missing.push('Festival'); } else festivalBase.classList.remove('is-invalid');

            if (!isValid) {
                SmartPop.error('Pricing Missing', errorMsg + missing.join(', '));
                return false;
            }

            // Logic Check: Weekend should usually be higher than Weekday
            if (parseFloat(weekendBase.value) < parseFloat(weekdayBase.value)) {
                const result = await SmartPop.confirm('Pricing Warning', 'Your Weekend Base Rate is LOWER than your Weekday Base Rate. Is this correct?', 'Yes, it is correct', 'No, let me change it');
                if (!result.isConfirmed) {
                    return false;
                }
            }
        }
    }

    if (step === 5) {
        const accountHolder = document.getElementById('account-holder');
        const accountNumber = document.getElementById('account-number');
        const ifscCode = document.getElementById('ifsc-code');
        const terms = document.getElementById('terms');

        let isValid = true;

        if (!accountHolder.value.trim()) { accountHolder.classList.add('is-invalid'); isValid = false; } else accountHolder.classList.remove('is-invalid');
        if (!accountNumber.value.trim()) { accountNumber.classList.add('is-invalid'); isValid = false; } else accountNumber.classList.remove('is-invalid');
        if (!ifscCode.value.trim()) { ifscCode.classList.add('is-invalid'); isValid = false; } else ifscCode.classList.remove('is-invalid');

        if (!terms.checked) {
            SmartPop.warning('Terms Required', 'You must agree to the Terms of Service and Privacy Policy.');
            return false;
        }

        if (!isValid) {
            SmartPop.error('Bank Details Missing', 'Please fill in all required bank details.');
            return false;
        }

        // Don't call submitForm here, let the submit button handler do it
        return true; // Allow validation to pass
    }

    return true;
}

function submitForm() {
    const form = document.querySelector('#vendor-registration-form');
    if (!form) {
        console.error('Form not found!');
        SmartPop.error('Form Error', 'Error: Registration form not found. Please refresh the page and try again.');
        return;
    }

    // Debug: Log selected category
    console.log('Selected Category:', selectedCategory);
    console.log('Form before adding service data:', form);

    // Add hidden inputs for Service Data
    if (selectedCategory) {
        const serviceTypeInput = document.createElement('input');
        serviceTypeInput.type = 'hidden';
        serviceTypeInput.name = 'Services[0].ServiceType';
        const categoryName = SERVICE_CATEGORIES.find(c => c.id === selectedCategory)?.name || '';
        serviceTypeInput.value = categoryName;
        console.log('Service Type:', categoryName);
        form.appendChild(serviceTypeInput);

        const costInput = document.createElement('input');
        costInput.type = 'hidden';
        costInput.name = 'Services[0].Cost';
        const basePrice = document.getElementById('base-price')?.value ||
            document.getElementById('weekday-base')?.value ||
            document.getElementById('weekend-base')?.value ||
            document.getElementById('festival-base')?.value ||
            0;
        costInput.value = basePrice;
        console.log('Service Cost:', basePrice);
        form.appendChild(costInput);
    } else {
        console.warn('No category selected! Service data will not be saved.');
    }

    // Collect Service Locations from added badges - extract pin codes
    const locationBadges = document.querySelectorAll('#locations-display .location-badge');
    const pinCodes = []; // Store pin codes from the location badges

    console.log('Found location badges:', locationBadges.length);

    // Get pin codes from the location badges
    locationBadges.forEach((badge) => {
        // First try to get from data attribute
        const pinCode = badge.getAttribute('data-pincode');
        if (pinCode) {
            pinCodes.push(pinCode);
            console.log('Added pin code from data attribute:', pinCode);
        } else {
            // Fallback: extract from text content
            const locationText = badge.querySelector('span')?.textContent.replace('📍 ', '').trim();
            if (locationText) {
                const pinCodeMatch = locationText.match(/\((\d{6})\)/);
                if (pinCodeMatch) {
                    pinCodes.push(pinCodeMatch[1]);
                    console.log('Extracted pin code from text:', pinCodeMatch[1]);
                }
            }
        }
    });

    // Also add the main vendor pin code if not already in the list
    const mainPinCode = document.querySelector('input[name="PinCode"]')?.value;
    if (mainPinCode && mainPinCode.trim() && !pinCodes.includes(mainPinCode.trim())) {
        pinCodes.push(mainPinCode.trim());
        console.log('Added main vendor pin code:', mainPinCode);
    }

    // If no pin codes were found, at least add the main vendor pin code
    if (pinCodes.length === 0 && mainPinCode && mainPinCode.trim()) {
        pinCodes.push(mainPinCode.trim());
        console.log('No service locations found, using main vendor pin code:', mainPinCode);
    }

    console.log('Total pin codes to add:', pinCodes.length, pinCodes);

    // Add hidden inputs for each pin code
    // ASP.NET Core model binding requires sequential indices starting from 0
    if (pinCodes.length > 0) {
        pinCodes.forEach((pinCode, index) => {
            if (pinCode && pinCode.trim()) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = `ServiceLocations[${index}]`;
                input.value = pinCode.trim();
                form.appendChild(input);
                console.log(`✓ Added ServiceLocations[${index}] = ${pinCode.trim()}`);
            }
        });
    } else {
        console.error('ERROR: No pin codes to add! Check if locations were added or main pin code is set.');
    }

    // Collect Attributes (Subcategories + Custom Fields + Pricing Model + Dynamic Pricing)
    const attributes = {
        subCategories: selectedSubCategories,
        pricingModel: document.querySelector('input[name="pricingModel"]:checked')?.value,
        customFields: {},
        dynamicPricing: null
    };

    // Collect custom fields
    const customFieldsMap = {};
    document.querySelectorAll('[name^="CustomFields"]').forEach(input => {
        const key = input.name.match(/\[(.*?)\]/)[1];

        if (input.type === 'checkbox') {
            // Handle multiple checkboxes with same name (multiselect)
            if (input.checked) {
                if (!customFieldsMap[key]) {
                    customFieldsMap[key] = [];
                }
                customFieldsMap[key].push(input.value);
            }
        } else if (input.type === 'radio') {
            if (input.checked) customFieldsMap[key] = input.value;
        } else if (input.tagName === 'SELECT' && input.multiple) {
            customFieldsMap[key] = Array.from(input.selectedOptions).map(opt => opt.value);
        } else {
            customFieldsMap[key] = input.value;
        }
    });
    attributes.customFields = customFieldsMap;

    // Collect dynamic pricing data if enabled
    const dynamicPricingEnabled = document.getElementById('enable-dynamic-pricing')?.checked;
    if (dynamicPricingEnabled) {
        const selectedFestivals = Array.from(document.querySelectorAll('.festival-checkbox:checked'))
            .map(cb => cb.value);

        attributes.dynamicPricing = {
            enabled: true,
            weekday: {
                base: parseFloat(document.getElementById('weekday-base')?.value) || 0,
                discounted: parseFloat(document.getElementById('weekday-discount')?.value) || null
            },
            weekend: {
                base: parseFloat(document.getElementById('weekend-base')?.value) || 0,
                discounted: parseFloat(document.getElementById('weekend-discount')?.value) || null
            },
            festival: {
                base: parseFloat(document.getElementById('festival-base')?.value) || 0,
                discounted: parseFloat(document.getElementById('festival-discount')?.value) || null,
                festivals: selectedFestivals
            }
        };
    }

    const attributesInput = document.createElement('input');
    attributesInput.type = 'hidden';
    attributesInput.name = 'Services[0].Attributes';
    attributesInput.value = JSON.stringify(attributes);
    form.appendChild(attributesInput);

    // Debug: Log all form data before submission
    console.log('Form data before submission:');
    const formData = new FormData(form);
    for (let [key, value] of formData.entries()) {
        console.log(key, ':', value);
    }

    // Verify service data is present
    const serviceTypeCheck = form.querySelector('input[name="Services[0].ServiceType"]');
    const serviceCostCheck = form.querySelector('input[name="Services[0].Cost"]');
    const serviceAttrsCheck = form.querySelector('input[name="Services[0].Attributes"]');

    console.log('Service Type Input:', serviceTypeCheck?.value);
    console.log('Service Cost Input:', serviceCostCheck?.value);
    console.log('Service Attributes Input:', serviceAttrsCheck?.value);

    // Verify ServiceLocations inputs
    const serviceLocationInputs = form.querySelectorAll('input[name^="ServiceLocations["]');
    console.log('ServiceLocations inputs found:', serviceLocationInputs.length);
    serviceLocationInputs.forEach((input, idx) => {
        console.log(`ServiceLocations[${idx}]:`, input.value);
    });

    // Ensure all inputs are in the DOM before submission
    if (serviceTypeCheck) {
        console.log('Service Type found in form');
    } else {
        console.error('Service Type NOT found in form!');
    }

    if (serviceLocationInputs.length === 0) {
        console.warn('WARNING: No ServiceLocations inputs found in form!');
    }

    // Show loading state
    const submitButton = document.getElementById('btn-submit');
    if (submitButton) {
        submitButton.disabled = true;
        const originalText = submitButton.innerHTML;
        submitButton.innerHTML = 'Submitting... <span class="spinner-border spinner-border-sm" role="status"></span>';

        // Re-enable button if submission fails (after timeout)
        setTimeout(() => {
            if (submitButton.disabled) {
                submitButton.disabled = false;
                submitButton.innerHTML = originalText;
            }
        }, 10000); // 10 second timeout
    }

    // Wait a moment to ensure all inputs are in the DOM
    setTimeout(() => {
        // Double-check ServiceLocations inputs are in the form
        const finalCheck = form.querySelectorAll('input[name^="ServiceLocations["]');
        console.log('Final check - ServiceLocations inputs before submit:', finalCheck.length);
        finalCheck.forEach((input, idx) => {
            console.log(`Final ServiceLocations[${idx}]:`, input.value);
        });

        // Ensure form action is correct (with base path)
        if (!form.action || form.action.includes('Register') === false) {
            const basePath = window.basePath || '/PartyClap/';
            form.action = basePath + 'Vendor/Register';
            form.method = 'post';
            console.log('Set form action to:', form.action);
        }

        console.log('Submitting form to:', form.action);
        console.log('Form method:', form.method);
        console.log('Form enctype:', form.enctype);

        // Mark as programmatic submit to allow it through
        form.dataset.programmaticSubmit = 'true';

        // Use requestSubmit instead of submit to ensure all inputs are included
        // requestSubmit triggers validation and includes all form fields
        try {
            if (form.requestSubmit) {
                // requestSubmit will trigger the submit event, but we've marked it as programmatic
                form.requestSubmit();
            } else {
                // Fallback: directly submit
                form.submit();
            }
        } catch (error) {
            console.error('Error submitting form:', error);
            SmartPop.error('Submission Failed', 'Error submitting form: ' + error.message + '\nPlease check the console for details.');
            if (submitButton) {
                submitButton.disabled = false;
                submitButton.innerHTML = 'Complete Registration 🎉';
            }
        }
    }, 100);
}


// Category Functions
function renderCategories() {
    const container = document.getElementById('category-grid');
    if (!container) return;

    container.innerHTML = '';

    SERVICE_CATEGORIES.forEach(category => {
        const col = document.createElement('div');
        col.className = 'col-6 col-md-4 col-lg-3'; // Responsive grid: 2 cols on mobile, 3 on tablet, 4 on desktop

        const card = document.createElement('div');
        card.className = `category-card ${selectedCategory === category.id ? 'selected' : ''}`;
        card.setAttribute('role', 'button');
        card.setAttribute('tabindex', '0');
        card.setAttribute('aria-label', `Select ${category.name} category`);

        // Add event listener directly
        card.addEventListener('click', () => selectCategory(category.id));

        // Add keyboard support
        card.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                selectCategory(category.id);
            }
        });

        const icon = document.createElement('div');
        icon.className = 'category-icon';
        icon.setAttribute('aria-hidden', 'true');
        icon.innerText = category.icon; // Use innerText for emoji

        const title = document.createElement('h6');
        title.className = 'mb-0';
        title.innerText = category.name;

        card.appendChild(icon);
        card.appendChild(title);
        col.appendChild(card);
        container.appendChild(col);
    });
}

function selectCategory(id) {
    selectedCategory = id;
    selectedSubCategories = []; // Reset subcategories
    renderCategories(); // Re-render to show selection
    renderSubCategories(id);
    renderCustomFields(id);
    renderPricingModels(id);
}

function renderSubCategories(catId) {
    const container = document.getElementById('subcategory-container');
    const wrapper = document.getElementById('subcategory-wrapper');
    if (!container || !wrapper) return;

    const category = SERVICE_CATEGORIES.find(c => c.id === catId);
    if (!category) return;

    wrapper.style.display = 'block';
    container.innerHTML = '';

    category.subCategories.forEach(sub => {
        const div = document.createElement('div');
        div.className = 'col-md-6 mb-3';
        div.innerHTML = `
            <div class="p-3 border rounded cursor-pointer subcategory-item" onclick="toggleSubCategory(this, '${sub.id}')">
                <div class="d-flex align-items-center">
                    <input type="checkbox" class="form-check-input me-3" id="sub-${sub.id}" ${selectedSubCategories.includes(sub.id) ? 'checked' : ''}>
                    <div>
                        <div class="fw-bold">${sub.name}</div>
                        <div class="text-muted small">${sub.description}</div>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(div);
    });
}

function toggleSubCategory(element, id) {
    const checkbox = element.querySelector('input[type="checkbox"]');
    checkbox.checked = !checkbox.checked;

    if (checkbox.checked) {
        selectedSubCategories.push(id);
        element.classList.add('bg-light', 'border-primary');
    } else {
        selectedSubCategories = selectedSubCategories.filter(item => item !== id);
        element.classList.remove('bg-light', 'border-primary');
    }
}

function renderCustomFields(catId) {
    const container = document.getElementById('custom-fields-container');
    if (!container) return;

    const category = SERVICE_CATEGORIES.find(c => c.id === catId);
    if (!category || !category.customFields) {
        container.innerHTML = '';
        return;
    }

    container.innerHTML = '<h5 class="mb-3">Additional Details</h5>';

    category.customFields.forEach(field => {
        const div = document.createElement('div');
        div.className = 'mb-4';

        const label = document.createElement('label');
        label.className = 'vendor-form-label';
        label.innerText = field.label;
        div.appendChild(label);

        if (field.type === 'text' || field.type === 'number') {
            const input = document.createElement('input');
            input.type = field.type;
            input.className = 'form-control vendor-form-input';
            input.placeholder = field.placeholder || '';
            input.id = `field-${field.id}`;
            input.name = `CustomFields[${field.id}]`; // For form submission
            div.appendChild(input);
        } else if (field.type === 'multiselect') {
            // Create checkbox grid instead of multiselect dropdown
            const checkboxContainer = document.createElement('div');
            checkboxContainer.className = 'row g-2';
            checkboxContainer.id = `field-${field.id}`;

            if (field.options) {
                field.options.forEach((opt, index) => {
                    const col = document.createElement('div');
                    col.className = 'col-6 col-md-4';

                    const checkboxWrapper = document.createElement('div');
                    checkboxWrapper.className = 'form-check p-3 border rounded cursor-pointer checkbox-option';
                    checkboxWrapper.style.cssText = 'transition: all 0.2s ease;';

                    const checkbox = document.createElement('input');
                    checkbox.type = 'checkbox';
                    checkbox.className = 'form-check-input';
                    checkbox.id = `${field.id}-${index}`;
                    checkbox.name = `CustomFields[${field.id}]`;
                    checkbox.value = opt;

                    const checkboxLabel = document.createElement('label');
                    checkboxLabel.className = 'form-check-label w-100 cursor-pointer';
                    checkboxLabel.htmlFor = `${field.id}-${index}`;
                    checkboxLabel.innerText = opt;

                    // Make entire div clickable
                    checkboxWrapper.addEventListener('click', function (e) {
                        if (e.target !== checkbox) {
                            checkbox.checked = !checkbox.checked;
                            updateCheckboxStyle(checkboxWrapper, checkbox.checked);
                        } else {
                            updateCheckboxStyle(checkboxWrapper, checkbox.checked);
                        }
                    });

                    checkboxWrapper.appendChild(checkbox);
                    checkboxWrapper.appendChild(checkboxLabel);
                    col.appendChild(checkboxWrapper);
                    checkboxContainer.appendChild(col);
                });
            }
            div.appendChild(checkboxContainer);

            const helper = document.createElement('small');
            helper.className = 'text-muted mt-2 d-block';
            helper.innerText = 'Select all that apply';
            div.appendChild(helper);
        }

        container.appendChild(div);
    });
}

// Helper function to update checkbox wrapper styling
function updateCheckboxStyle(wrapper, isChecked) {
    if (isChecked) {
        wrapper.classList.add('bg-light', 'border-primary');
        wrapper.style.borderWidth = '2px';
    } else {
        wrapper.classList.remove('bg-light', 'border-primary');
        wrapper.style.borderWidth = '1px';
    }
}

function renderPricingModels(catId) {
    const container = document.getElementById('pricing-model-container');
    if (!container) return;

    const category = SERVICE_CATEGORIES.find(c => c.id === catId);
    if (!category) return;

    container.innerHTML = '';

    category.pricingModels.forEach(model => {
        const div = document.createElement('div');
        div.className = 'col-md-6 mb-3';
        div.innerHTML = `
            <div class="p-3 border rounded cursor-pointer pricing-model-item" onclick="selectPricingModel(this, '${model.id}')">
                <div class="d-flex align-items-center">
                    <input type="radio" name="pricingModel" class="form-check-input me-3" id="model-${model.id}" value="${model.id}">
                    <div>
                        <div class="fw-bold">${model.name}</div>
                        <div class="text-muted small">${model.description}</div>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(div);
    });
}

function selectPricingModel(element, id) {
    // Deselect all
    document.querySelectorAll('.pricing-model-item').forEach(el => {
        el.classList.remove('bg-light', 'border-primary');
        el.querySelector('input').checked = false;
    });

    // Select current
    element.classList.add('bg-light', 'border-primary');
    element.querySelector('input').checked = true;
}

function initStateCityCascade(container = document) {
    const stateDropdowns = container.querySelectorAll('select[data-cascade="state"]:not(.cascade-initialized)');
    console.log(`Found ${stateDropdowns.length} state dropdown(s) to initialize`);

    stateDropdowns.forEach(stateSelect => {
        stateSelect.classList.add('cascade-initialized');
        stateSelect.addEventListener('change', async function () {
            const selectedState = this.value;
            console.log('State changed to:', selectedState);

            // Try multiple ways to find the city dropdown
            let citySelect = null;
            const parent = this.closest('.row') || this.closest('.g-3') || this.closest('form') || this.parentElement;
            if (parent) {
                citySelect = parent.querySelector('select[data-cascade="city"]');
            }

            // If still not found, try searching in the same form/container
            if (!citySelect) {
                const form = this.closest('form');
                if (form) {
                    citySelect = form.querySelector('select[data-cascade="city"]');
                }
            }

            // For step-1, try specific selector
            if (!citySelect && this.closest('#step-1')) {
                citySelect = document.querySelector('#step-1 select[data-cascade="city"]');
            }

            if (citySelect) {
                console.log('Found city dropdown');
                citySelect.innerHTML = '<option value="">Loading cities...</option>';
                citySelect.disabled = true;

                if (selectedState) {
                    try {
                        console.log('Loading cities for state:', selectedState);
                        const cities = await loadCitiesForState(selectedState);
                        console.log('Received cities:', cities);
                        citySelect.innerHTML = '<option value="">Select your city</option>';

                        if (cities && Array.isArray(cities) && cities.length > 0) {
                            cities.forEach(city => {
                                if (city && typeof city === 'string') {
                                    const option = document.createElement('option');
                                    option.value = city;
                                    option.textContent = city;
                                    citySelect.appendChild(option);
                                }
                            });
                            citySelect.disabled = false;
                            console.log(`Populated ${cities.length} cities`);
                        } else {
                            console.warn('No cities returned for state:', selectedState);
                            citySelect.innerHTML = '<option value="">No cities found</option>';
                            citySelect.disabled = true;
                        }
                    } catch (error) {
                        console.error('Error loading cities:', error);
                        citySelect.innerHTML = '<option value="">Error loading cities</option>';
                        citySelect.disabled = true;
                    }
                } else {
                    citySelect.innerHTML = '<option value="">Select your city</option>';
                    citySelect.disabled = true;
                }
            } else {
                console.error('City dropdown not found for state:', selectedState);
            }
        });
    });
}

function initCityPinCodeAutoPopulate() {
    // For the location adder specifically
    const citySelect = document.querySelector('#locations-container select[data-cascade="city"]');
    const pinCodeInput = document.getElementById('location-pincode');

    if (citySelect && pinCodeInput) {
        citySelect.addEventListener('change', async function () {
            const selectedCity = this.value;
            if (selectedCity) {
                // Load pin codes for this city
                await loadPinCodesForCity(selectedCity);
                if (cityPinCodeMap[selectedCity]) {
                    pinCodeInput.value = cityPinCodeMap[selectedCity];
                } else {
                    pinCodeInput.value = '';
                }
            } else {
                pinCodeInput.value = '';
            }
        });

        // Add autocomplete for location pincode input
        initPinCodeAutocomplete(pinCodeInput, 'location-pincode-autocomplete');
    }

    // For the main address section
    const mainCitySelect = document.querySelector('#step-1 select[data-cascade="city"]');
    const mainPinCodeInput = document.getElementById('main-pincode-input') || document.querySelector('#step-1 input[name="PinCode"]');

    if (mainCitySelect && mainPinCodeInput) {
        mainCitySelect.addEventListener('change', async function () {
            const selectedCity = this.value;
            if (selectedCity) {
                // Load pin codes for this city
                await loadPinCodesForCity(selectedCity);
                if (cityPinCodeMap[selectedCity]) {
                    mainPinCodeInput.value = cityPinCodeMap[selectedCity];
                }
            }
        });
    }

    // Add pin code autocomplete functionality (works independently)
    if (mainPinCodeInput) {
        initPinCodeAutocomplete(mainPinCodeInput, 'pincode-autocomplete');
    }
}

function initLocationAdder() {
    const btnAdd = document.getElementById('btn-add-location');
    const stateSelect = document.querySelector('#locations-container select[data-cascade="state"]');
    const citySelect = document.querySelector('#locations-container select[data-cascade="city"]');
    const pinCodeInput = document.getElementById('location-pincode');
    const locationsDisplay = document.getElementById('locations-display');
    const addedLocationsList = document.getElementById('added-locations-list');

    // Array to store added locations
    let addedLocations = [];

    if (btnAdd && stateSelect && citySelect) {
        btnAdd.addEventListener('click', function () {
            const selectedState = stateSelect.value;
            const selectedCity = citySelect.value;
            const selectedPinCode = pinCodeInput ? pinCodeInput.value.trim() : '';

            // Validate selection
            if (!selectedState || !selectedCity) {
                SmartPop.warning('Location Incomplete', 'Please select both State and City before adding a location.');
                return;
            }

            // Validate Pin Code if entered
            if (selectedPinCode && !/^\d{6}$/.test(selectedPinCode)) {
                SmartPop.warning('Invalid Pin Code', 'Please enter a valid 6-digit Pin Code.');
                return;
            }

            // Check for duplicates
            const locationKey = selectedPinCode
                ? `${selectedCity}, ${selectedState} - ${selectedPinCode}`
                : `${selectedCity}, ${selectedState}`;

            if (addedLocations.some(loc => loc.key === locationKey)) {
                SmartPop.warning('Duplicate Location', 'This location has already been added.');
                return;
            }

            // Determine pin code to use - check database map first, then fallback
            const pinCodeToUse = selectedPinCode || cityPinCodeMap[selectedCity] || cityPinCodeData[selectedCity] || '';

            // Add to array
            addedLocations.push({
                key: locationKey,
                state: selectedState,
                city: selectedCity,
                pinCode: pinCodeToUse
            });

            // Create badge element
            const badge = document.createElement('div');
            badge.className = 'location-badge';
            badge.style.cssText = 'background: linear-gradient(135deg, #8B5CF6 0%, #EC4899 100%); color: white; padding: 8px 16px; border-radius: 20px; font-size: 14px; font-weight: 500; display: inline-flex; align-items: center; gap: 8px;';

            // Store pin code in data attribute for easy retrieval
            //const pinCodeToUse1 = selectedPinCode || cityPinCodeData[selectedCity] || '';
            if (pinCodeToUse) {
                badge.setAttribute('data-pincode', pinCodeToUse);
            }

            const displayText = pinCodeToUse
                ? `📍 ${selectedCity}, ${selectedState} (${pinCodeToUse})`
                : `📍 ${selectedCity}, ${selectedState}`;

            badge.innerHTML = `
                <span>${displayText}</span>
                <button type="button" class="btn-close btn-close-white" style="font-size: 10px; opacity: 0.8;" title="Remove"></button>
            `;

            // Add remove functionality
            badge.querySelector('.btn-close').addEventListener('click', function () {
                addedLocations = addedLocations.filter(loc => loc.key !== locationKey);
                badge.remove();

                // Hide list if empty
                if (addedLocations.length === 0) {
                    addedLocationsList.style.display = 'none';
                }
            });

            // Add to display
            locationsDisplay.appendChild(badge);

            // Show the list
            addedLocationsList.style.display = 'block';

            // Reset dropdowns and input
            stateSelect.value = '';
            citySelect.value = '';
            citySelect.disabled = true;
            if (pinCodeInput) pinCodeInput.value = '';

            // Success feedback
            const originalText = btnAdd.textContent;
            btnAdd.textContent = '✓ Location Added!';
            setTimeout(() => {
                btnAdd.textContent = originalText;
            }, 2000);
        });
    }
}

// Pin Code Autocomplete Function
function initPinCodeAutocomplete(inputElement, dropdownId) {
    if (!inputElement) return;

    const dropdown = document.getElementById(dropdownId);
    if (!dropdown) {
        console.warn(`Dropdown with id '${dropdownId}' not found`);
        return;
    }

    let searchTimeout;
    let currentHighlight = -1;
    let currentResults = [];

    // Hide dropdown when clicking outside
    document.addEventListener('click', function (e) {
        if (!inputElement.contains(e.target) && !dropdown.contains(e.target)) {
            hideDropdown();
        }
    });

    // Input event handler
    inputElement.addEventListener('input', async function () {
        const searchTerm = this.value.trim();

        // Clear previous timeout
        clearTimeout(searchTimeout);

        // Hide dropdown if input is empty or is a valid 6-digit pin code
        if (searchTerm.length === 0) {
            hideDropdown();
            return;
        }

        // If it's a valid 6-digit pin code, don't show suggestions
        if (searchTerm.length === 6 && /^\d{6}$/.test(searchTerm)) {
            hideDropdown();
            return;
        }

        // Search after user stops typing (debounce)
        if (searchTerm.length >= 2) {
            searchTimeout = setTimeout(async () => {
                await performSearch(searchTerm);
            }, 300);
        } else {
            hideDropdown();
        }
    });

    // Keyboard navigation
    inputElement.addEventListener('keydown', function (e) {
        if (!dropdown.style.display || dropdown.style.display === 'none') return;

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            currentHighlight = Math.min(currentHighlight + 1, currentResults.length - 1);
            updateHighlight();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            currentHighlight = Math.max(currentHighlight - 1, -1);
            updateHighlight();
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (currentHighlight >= 0 && currentHighlight < currentResults.length) {
                selectLocation(currentResults[currentHighlight]);
            }
        } else if (e.key === 'Escape') {
            hideDropdown();
        }
    });

    async function performSearch(searchTerm) {
        try {
            const locations = await searchPinCodes(searchTerm);
            currentResults = locations;
            currentHighlight = -1;

            if (locations.length > 0) {
                displayResults(locations);
            } else {
                showNoResults();
            }
        } catch (error) {
            console.error('Error searching pin codes:', error);
            hideDropdown();
        }
    }

    function displayResults(locations) {
        dropdown.innerHTML = '';
        if (!Array.isArray(locations) || locations.length === 0) {
            showNoResults();
            return;
        }

        locations.forEach((location, index) => {
            if (!location) return;

            const item = document.createElement('div');
            item.className = 'pincode-autocomplete-item';
            item.setAttribute('data-index', index);

            // Handle both uppercase and lowercase property names
            const pinCode = location.PinCode || location.pinCode || location.pincode || '';
            const areaName = location.AreaName || location.areaName || location.area || '';
            const city = location.City || location.city || '';
            const state = location.State || location.state || '';
            const details = [areaName, city, state].filter(x => x && x !== 'undefined').join(', ');

            if (!pinCode) {
                console.warn('Location missing PinCode:', location);
                return;
            }

            item.innerHTML = `
                <div class="pincode-location">
                    <div>
                        <div class="pincode-value">${pinCode}</div>
                        <div class="pincode-details">${details || 'Location details not available'}</div>
                    </div>
                </div>
            `;

            item.addEventListener('click', () => selectLocation(location));
            item.addEventListener('mouseenter', () => {
                currentHighlight = index;
                updateHighlight();
            });

            dropdown.appendChild(item);
        });

        if (dropdown.children.length > 0) {
            showDropdown();
        } else {
            showNoResults();
        }
    }

    function showNoResults() {
        dropdown.innerHTML = '<div class="pincode-autocomplete-no-results">No locations found</div>';
        showDropdown();
    }

    function selectLocation(location) {
        if (!location) return;

        // Handle both uppercase and lowercase property names
        const pinCode = location.PinCode || location.pinCode || location.pincode || '';
        const city = location.City || location.city || '';
        const state = location.State || location.state || '';

        if (pinCode) {
            inputElement.value = pinCode;
        }
        hideDropdown();

        // Optionally update city and state if they exist
        const parent = inputElement.closest('.row') || inputElement.closest('.g-3') || inputElement.closest('form');
        const citySelect = parent?.querySelector('select[data-cascade="city"]') || document.querySelector('#step-1 select[data-cascade="city"]');
        const stateSelect = parent?.querySelector('select[data-cascade="state"]') || document.querySelector('#step-1 select[data-cascade="state"]');

        if (city && citySelect) {
            citySelect.value = city;
            // Trigger change event to update pincode if needed
            citySelect.dispatchEvent(new Event('change'));
        }

        if (state && stateSelect) {
            stateSelect.value = state;
            // Trigger change event to load cities
            stateSelect.dispatchEvent(new Event('change'));
        }

        // Trigger input event to notify other handlers
        inputElement.dispatchEvent(new Event('input'));
    }

    function updateHighlight() {
        const items = dropdown.querySelectorAll('.pincode-autocomplete-item');
        items.forEach((item, index) => {
            if (index === currentHighlight) {
                item.classList.add('highlighted');
                item.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
            } else {
                item.classList.remove('highlighted');
            }
        });
    }

    function showDropdown() {
        dropdown.style.display = 'block';
    }

    function hideDropdown() {
        dropdown.style.display = 'none';
        currentHighlight = -1;
        currentResults = [];
    }
}
