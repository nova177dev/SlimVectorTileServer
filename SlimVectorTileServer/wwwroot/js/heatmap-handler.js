/**
 * Heatmap Handler - Manages Mapbox heatmap visualization and vector tile interactions
 */

// Configuration
const CONFIG = {
    API_BASE_URL: '',
    MAP_SETTINGS: {
        initialCenter: [-74.6071028, 40.6931568],
        initialZoom: 2,
        minZoom: 2,
        maxZoom: 15
    },
    LAYER_SETTINGS: {
        circleRadius: 3,
        circleColor: '#ff0000'
    },
    HEATMAP_SETTINGS: {
        // Only show heatmap at zoom levels where it's most effective
        minZoom: 2,
        maxZoom: 9,
        // Optimize performance by limiting radius and intensity
        maxRadius: 15,
        maxIntensity: 2
    }
};

// Mapbox access token - Should be stored in a secure configuration
// In a production environment, this should be loaded from environment variables or a secure backend
const MAPBOX_TOKEN = '';
mapboxgl.accessToken = MAPBOX_TOKEN;

// Initialize map
const map = new mapboxgl.Map({
    container: 'map',
    style: 'mapbox://styles/mapbox/streets-v11',
    center: CONFIG.MAP_SETTINGS.initialCenter,
    zoom: CONFIG.MAP_SETTINGS.initialZoom,
    minZoom: CONFIG.MAP_SETTINGS.minZoom,
    maxZoom: CONFIG.MAP_SETTINGS.maxZoom,
    renderWorldCopies: false
});

/**
 * Convert longitude and latitude to tile coordinates
 * @param {number} lng - Longitude
 * @param {number} lat - Latitude
 * @param {number} zoom - Zoom level
 * @returns {Object} Tile coordinates {x, y}
 */
function lngLatToTile(lng, lat, zoom) {
    const tileCount = Math.pow(2, zoom);
    const x = Math.floor((lng + 180) / 360 * tileCount);
    const y = Math.floor((1 - Math.log(Math.tan(lat * Math.PI / 180) + 1 / Math.cos(lat * Math.PI / 180)) / Math.PI) / 2 * tileCount);
    return { x, y };
}

/**
 * Clean up existing map layers and sources
 */
function cleanUpMap() {
    if (map.getLayer('sites-layer')) {
        map.removeLayer('sites-layer');
    }
    if (map.getLayer('sites-heat')) {
        map.removeLayer('sites-heat');
    }
    if (map.getSource('sites-source')) {
        map.removeSource('sites-source');
    }
}

/**
 * Display a notification message to the user
 * @param {string} message - The message to display
 * @param {string} type - The type of message ('error' or 'success')
 */
function showNotification(message, type = 'error') {
    console.log(`${type.toUpperCase()}: ${message}`);
    // In a production app, you might want to add a visual notification here
}

/**
 * Update map tiles with the given UUID
 * @param {string} uuid - UUID for the tile request
 */
function updateTiles(uuid) {
    const tilesUrlTemplate = `${CONFIG.API_BASE_URL}/tiles/{z}/{x}/{y}/${uuid}.mvt`;

    try {
        map.addSource('sites-source', {
            type: 'vector',
            tiles: [tilesUrlTemplate],
            minzoom: CONFIG.MAP_SETTINGS.minZoom,
            maxzoom: CONFIG.MAP_SETTINGS.maxZoom + 7 // Allow higher zoom for detailed viewing
        });
    }
    catch (error) {
        showNotification(`Failed to add vector tile source: ${error.message}`);
        return false;
    }
try {
    map.addLayer(
        {
            'id': 'sites-heat',
            'type': 'heatmap',
            'source': 'sites-source',
            'source-layer': 'sites',
            'minzoom': CONFIG.HEATMAP_SETTINGS.minZoom,
            'maxzoom': CONFIG.HEATMAP_SETTINGS.maxZoom,
            'paint': {
                // Increase the heatmap weight based on frequency and property magnitude
                // Optimize weight calculation - use a threshold to filter out low-weight points
                'heatmap-weight': [
                    'interpolate',
                    ['linear'],
                    ['get', 'mag'],
                    0,
                    0,
                    3, // Lower threshold for better performance
                    0.5,
                    6,
                    1
                ],
                // Increase the heatmap color weight by zoom level
                // heatmap-intensity is a multiplier on top of heatmap-weight
                // Limit intensity for better performance
                'heatmap-intensity': [
                    'interpolate',
                    ['linear'],
                    ['zoom'],
                    0,
                    0.7, // Lower base intensity
                    CONFIG.HEATMAP_SETTINGS.maxZoom,
                    CONFIG.HEATMAP_SETTINGS.maxIntensity
                ],
                // Color ramp for heatmap.  Domain is 0 (low) to 1 (high).
                // Begin color ramp at 0-stop with a 0-transparency color
                // to create a blur-like effect.
                // Simplify color ramp with fewer stops for better performance
                'heatmap-color': [
                    'interpolate',
                    ['linear'],
                    ['heatmap-density'],
                    0,
                    'rgba(33,102,172,0)',
                    0.3,
                    'rgb(103,169,207)',
                    0.6,
                    'rgb(253,219,199)',
                    1,
                    'rgb(178,24,43)'
                ],
                // Adjust the heatmap radius by zoom level
                // Optimize radius for better performance
                'heatmap-radius': [
                    'interpolate',
                    ['linear'],
                    ['zoom'],
                    0,
                    1,
                    CONFIG.HEATMAP_SETTINGS.maxZoom,
                    CONFIG.HEATMAP_SETTINGS.maxRadius
                ],
                // Transition from heatmap to circle layer by zoom level
                // More abrupt transition for better performance
                'heatmap-opacity': [
                    'interpolate',
                    ['linear'],
                    ['zoom'],
                    CONFIG.HEATMAP_SETTINGS.maxZoom - 1,
                    1,
                    CONFIG.HEATMAP_SETTINGS.maxZoom,
                    0
                ]
            }
        },
        'waterway-label'
    );
}
catch (error) {
    showNotification(`Failed to add heatmap layer: ${error.message}`);
    return false;
}

try {
    map.addLayer(
        {
            'id': 'sites-layer',
            'type': 'circle',
            'source': 'sites-source',
            'source-layer': 'sites',
            'minzoom': CONFIG.HEATMAP_SETTINGS.maxZoom - 1, // Start showing points just before heatmap fades out
            'paint': {
                'circle-radius': CONFIG.LAYER_SETTINGS.circleRadius,
                'circle-color': CONFIG.LAYER_SETTINGS.circleColor
            }
        },
        'waterway-label'
    );
    return true;
}
catch (error) {
    showNotification(`Failed to add circle layer: ${error.message}`);
    return false;
}
    return true;
}

/**
 * Create request parameters and send to the server
 * @returns {Promise<string|null>} UUID from the server response or null if failed
 */
async function createRequestParams() {
    const requestParams = document.getElementById('requestParams').value;

    try {
        // Validate JSON input
        const parsedParams = JSON.parse(requestParams);

        // Add a limit parameter to reduce data points if not already specified
        if (!parsedParams.limit && !parsedParams.maxPoints) {
            // Suggest a reasonable limit based on device performance
            const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
            parsedParams.limit = isMobile ? 5000 : 10000;
        }

        const response = await fetch(`${CONFIG.API_BASE_URL}/tiles/request-params`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(parsedParams)
        });

        if (!response.ok) {
            throw new Error(`Failed with status: ${response.status} - ${response.statusText}`);
        }

        const result = await response.json();
        return result.data.uuid;
    } catch (error) {
        if (error instanceof SyntaxError) {
            showNotification('Invalid JSON format. Please check your input.');
        } else {
            showNotification(`Error: ${error.message}`);
        }
        return null;
    }
}

/**
 * Initialize the application
 */
function initApp() {
    // Add map controls
    map.addControl(new mapboxgl.NavigationControl(), 'top-right');
    map.addControl(new mapboxgl.FullscreenControl(), 'top-right');

    // Enable WebGL acceleration for better performance
    if (map.getCanvas()) {
        map.getCanvas().style.willChange = 'transform';
    }

    // Set up event listeners
    const submitButton = document.getElementById('submitButton');

    // Remove any existing event listeners to prevent duplicates
    const newSubmitButton = submitButton.cloneNode(true);
    submitButton.parentNode.replaceChild(newSubmitButton, submitButton);

    newSubmitButton.addEventListener('click', async () => {
        // Show loading indicator
        showNotification('Loading heatmap data...', 'info');

        const uuid = await createRequestParams();
        if (uuid) {
            cleanUpMap();
            const success = updateTiles(uuid);
            if (success) {
                showNotification('Map updated successfully', 'success');
            }
        } else {
            showNotification('Failed to get request parameters UUID');
        }
    });
}

// Initialize the application when the DOM is fully loaded
document.addEventListener('DOMContentLoaded', initApp);
