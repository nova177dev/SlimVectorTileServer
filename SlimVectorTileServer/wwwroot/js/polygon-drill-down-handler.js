/**
 * Polygon Map Handler - Manages Mapbox map and polygon vector tile interactions
 */

// Configuration
const CONFIG = {
    // Replace API_BASE_URL with the actual value in your case.
    API_BASE_URL: '',
    MAP_SETTINGS: {
        initialCenter: [-74.6071028, 40.6931568],
        initialZoom: 2,
        minZoom: 2,
        maxZoom: 15
    },
    LAYER_SETTINGS: {
        fillOpacity: 0.5,
        outlineColor: '#000',
        // Level-based fill colors
        levelColors: {
            0: '#1f78b4',  // Dark Blue
            1: '#1b9e77',  // Teal
            2: '#d95f02',  // Orange
            3: '#7570b3',  // Purple
            4: '#e7298a'   // Magenta
        },
        defaultFillColor: '#777777',
        // Area-based opacity settings (area in square meters from STArea())
        opacitySettings: {
            minOpacity: 0.5,      // Minimum opacity for smallest areas
            maxOpacity: 0.9,      // Maximum opacity for largest areas
            minArea: 1e9,         // ~1,000 km² (small countries/regions)
            maxArea: 1e13         // ~10,000,000 km² (large countries like Russia)
        }
    },
    FIT_BOUNDS_OPTIONS: {
        padding: 50,
        maxZoom: 15,
        duration: 1000
    }
};

// Replace this with your own token or implement a more secure approach.
// mapboxgl.accessToken = '@Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN")';
mapboxgl.accessToken = '';

// Initialize map
const map = new mapboxgl.Map({
    container: 'map',
    //style: 'mapbox://styles/mapbox/streets-v11',
    style: 'mapbox://styles/mapbox/standard',
    center: CONFIG.MAP_SETTINGS.initialCenter,
    zoom: CONFIG.MAP_SETTINGS.initialZoom,
    minZoom: CONFIG.MAP_SETTINGS.minZoom,
    maxZoom: CONFIG.MAP_SETTINGS.maxZoom,
    renderWorldCopies: false
});

// Track current popup for cleanup
let currentPopup = null;

// Track current request params for maintaining search_string across clicks
let currentSearchString = null;

function lngLatToTile(lng, lat, zoom) {
    const tileCount = Math.pow(2, zoom);
    const x = Math.floor((lng + 180) / 360 * tileCount);
    const y = Math.floor((1 - Math.log(Math.tan(lat * Math.PI / 180) + 1 / Math.cos(lat * Math.PI / 180)) / Math.PI) / 2 * tileCount);
    return { x, y };
}

function cleanUpMap() {
    if (map.getLayer('polygons-outline-layer')) {
        map.removeLayer('polygons-outline-layer');
    }
    if (map.getLayer('polygons-layer')) {
        map.removeLayer('polygons-layer');
    }
    if (map.getSource('polygons-source')) {
        map.removeSource('polygons-source');
    }
}

function closePopup() {
    if (currentPopup) {
        currentPopup.remove();
        currentPopup = null;
    }
}

function showNotification(message, type = 'error') {
    console.log(`${type.toUpperCase()}: ${message}`);
}

/**
 * Fetches polygon bounds from the API and fits the map to those bounds
 * @param {number} polygonId - The polygon ID to fetch bounds for
 */
async function fetchAndFitToPolygonBounds(polygonId) {
    try {
        const response = await fetch(`${CONFIG.API_BASE_URL}/tiles/polygons/bounds/${polygonId}`);

        if (!response.ok) {
            if (response.status === 404) {
                showNotification(`Polygon with ID ${polygonId} not found`);
            } else {
                showNotification(`Failed to fetch polygon bounds: ${response.statusText}`);
            }
            return false;
        }

        const data = await response.json();

        const bounds = [
            [data.boundsWest, data.boundsSouth], // Southwest corner [lng, lat]
            [data.boundsEast, data.boundsNorth]  // Northeast corner [lng, lat]
        ];

        map.fitBounds(bounds, CONFIG.FIT_BOUNDS_OPTIONS);
        showNotification(`Zoomed to: ${data.name}`, 'success');
        return true;
    } catch (error) {
        showNotification(`Error fetching polygon bounds: ${error.message}`);
        return false;
    }
}

function updateTiles(uuid) {
    const zoom = Math.floor(map.getZoom());
    const center = map.getCenter();
    const centerTile = lngLatToTile(center.lng, center.lat, zoom);
    const tilesUrlTemplate = `${CONFIG.API_BASE_URL}/tiles/polygons/{z}/{x}/{y}/${uuid}`;

    const { minOpacity, maxOpacity, minArea, maxArea } = CONFIG.LAYER_SETTINGS.opacitySettings;

    try {
        map.addSource('polygons-source', {
            type: 'vector',
            tiles: [tilesUrlTemplate],
            minzoom: CONFIG.MAP_SETTINGS.minZoom,
            maxzoom: CONFIG.MAP_SETTINGS.maxZoom
        });
    } catch (error) {
        showNotification(`Failed to add vector tile source: ${error.message}`);
        return false;
    }

    try {
        // Add fill layer for polygons with level-based coloring and area-based opacity
        map.addLayer({
            id: 'polygons-layer',
            type: 'fill',
            source: 'polygons-source',
            'source-layer': 'polygons',
            paint: {
                'fill-color': [
                    'match',
                    ['get', 'level'],
                    0, CONFIG.LAYER_SETTINGS.levelColors[0],
                    1, CONFIG.LAYER_SETTINGS.levelColors[1],
                    2, CONFIG.LAYER_SETTINGS.levelColors[2],
                    3, CONFIG.LAYER_SETTINGS.levelColors[3],
                    4, CONFIG.LAYER_SETTINGS.levelColors[4],
                    CONFIG.LAYER_SETTINGS.defaultFillColor // fallback color
                ],
                // Dynamic opacity based on area: larger area = higher opacity
                'fill-opacity': [
                    'interpolate',
                    ['linear'],
                    ['coalesce', ['get', 'area'], minArea], // Use area property, fallback to minArea
                    minArea, minOpacity,  // Small areas get minimum opacity
                    maxArea, maxOpacity   // Large areas get maximum opacity
                ]
            }
        });

        // Add outline layer for polygons
        map.addLayer({
            id: 'polygons-outline-layer',
            type: 'line',
            source: 'polygons-source',
            'source-layer': 'polygons',
            paint: {
                'line-color': CONFIG.LAYER_SETTINGS.outlineColor,
                'line-width': 0.5
            }
        });

        return true;
    } catch (error) {
        showNotification(`Failed to add vector tile layer: ${error.message}`);
        return false;
    }
}

/**
 * Creates request params with optional parent_id for drilling down into polygons
 * @param {number|null} parentId - The parent polygon ID to filter by (null for initial load)
 * @returns {Promise<string|null>} - The UUID for the request params or null on failure
 */
async function createRequestParamsWithParent(parentId = null) {
    try {
        // Build the inner params object with parent_id
        const innerParams = {
            parent_id: parentId
        };

        // Include search_string if available
        if (currentSearchString) {
            innerParams.search_string = currentSearchString;
        }

        // Wrap in 'data' property to match VectorTileRequestParams structure
        const requestBody = {
            data: innerParams
        };

        console.log('Sending request params:', JSON.stringify(requestBody));

        const response = await fetch(`${CONFIG.API_BASE_URL}/tiles/request-params`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestBody)
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

async function createRequestParams() {
    const requestParams = document.getElementById('requestParams').value.trim();

    try {
        let parsedParams;
        try {
            parsedParams = JSON.parse(requestParams);
        } catch (parseError) {
            showNotification('Invalid JSON format. Please check your input.');
            return null;
        }

        // Extract and store search_string from the data property for future polygon clicks
        if (parsedParams.data && parsedParams.data.search_string) {
            currentSearchString = parsedParams.data.search_string;
        } else if (parsedParams.search_string) {
            // Handle case where user provides flat structure - wrap it
            currentSearchString = parsedParams.search_string;
            parsedParams = { data: parsedParams };
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
 * Handles polygon click - cleans the map and reloads with clicked polygon as parent
 * @param {number} polygonId - The clicked polygon's ID
 */
async function handlePolygonClick(polygonId) {
    showNotification(`Drilling down into polygon ID: ${polygonId}`, 'info');

    // Create new request params with the clicked polygon as parent_id
    const uuid = await createRequestParamsWithParent(polygonId);

    if (uuid) {
        // Clean up existing map layers and sources
        cleanUpMap();

        // Fetch and fit to polygon bounds
        await fetchAndFitToPolygonBounds(polygonId);

        // Update tiles with new params (filtered by parent_id)
        if (updateTiles(uuid)) {
            showNotification(`Map updated - showing children of polygon ${polygonId}`, 'success');
        }
    } else {
        showNotification('Failed to create request parameters for polygon drill-down');
    }
}

function initApp() {
    map.addControl(new mapboxgl.NavigationControl(), 'top-right');
    map.addControl(new mapboxgl.FullscreenControl(), 'top-right');

    document.getElementById('submitButton').addEventListener('click', async () => {
        // Reset parent_id tracking when user manually submits
        currentSearchString = null;

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

    // Add click handler to drill down into polygon children
    map.on('click', 'polygons-layer', (e) => {
        if (e.features.length > 0) {
            const feature = e.features[0];
            const polygonId = feature.properties.id;

            if (polygonId) {
                closePopup();
                handlePolygonClick(polygonId);
            } else {
                showNotification('Polygon ID not available');
            }
        }
    });

    // Close popup on zoom change
    map.on('zoom', closePopup);

    // Change cursor on hover
    map.on('mouseenter', 'polygons-layer', () => {
        map.getCanvas().style.cursor = 'pointer';
    });

    map.on('mouseleave', 'polygons-layer', () => {
        map.getCanvas().style.cursor = '';
    });
}

document.addEventListener('DOMContentLoaded', initApp);