/**
 * Map Handler - Manages Mapbox map and vector tile interactions
 */

// Configuration
const CONFIG = {
    API_BASE_URL: 'http://localhost:5035/api',
    MAP_SETTINGS: {
        initialCenter: [-74.6071028, 40.6931568],
        initialZoom: 2,
        minZoom: 2,
        maxZoom: 15
    },
    LAYER_SETTINGS: {
        circleRadius: 3,
        circleColor: '#ff0000'
    }
};

// Mapbox access token - Should be stored in a secure configuration
// Replace this with your own token or implement a more secure approach
mapboxgl.accessToken = '[YOUR_MAPBOX_API_KEY]';

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
    const zoom = Math.floor(map.getZoom());
    const center = map.getCenter();
    const bounds = map.getBounds();
    const centerTile = lngLatToTile(center.lng, center.lat, zoom);
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
        map.addLayer({
            id: 'sites-layer',
            type: 'circle',
            source: 'sites-source',
            'source-layer': 'sites',
            paint: {
                'circle-radius': CONFIG.LAYER_SETTINGS.circleRadius,
                'circle-color': CONFIG.LAYER_SETTINGS.circleColor
            }
        });
        return true;
    }
    catch (error) {
        showNotification(`Failed to add vector tile layer: ${error.message}`);
        return false;
    }
}

/**
 * Create request parameters and send to the server
 * @returns {Promise<string|null>} UUID from the server response or null if failed
 */
async function createRequestParams() {
    const requestParams = document.getElementById('requestParams').value;

    try {
        // Validate JSON input
        let parsedParams;
        try {
            parsedParams = JSON.parse(requestParams);
        } catch (parseError) {
            showNotification('Invalid JSON format. Please check your input.');
            return null;
        }

        const response = await fetch(`${CONFIG.API_BASE_URL}/tiles/request-params`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(parsedParams)
        });

        if (response.ok) {
            const result = await response.json();
            return result.data.uuid;
        } else {
            throw new Error(`Failed with status: ${response.status} - ${response.statusText}`);
        }
    } catch (error) {
        showNotification(`Error: ${error.message}`);
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

    // Set up event listeners
    document.getElementById('submitButton').addEventListener('click', async () => {
        const uuid = await createRequestParams();
        if (uuid) {
            cleanUpMap();
            if (updateTiles(uuid)) {
                showNotification('Map updated successfully', 'success');
            }
        } else {
            showNotification('Failed to get request parameters UUID');
        }
    });
}

// Initialize the application when the DOM is fully loaded
document.addEventListener('DOMContentLoaded', initApp);