/**
 * Polygon Map Handler - Manages Mapbox map and polygon vector tile interactions
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
        fillOpacity: 0.5,
        outlineColor: '#000',
        // Level-based fill colors
        levelColors: {
            1: '#1b9e77',  // Teal
            2: '#d95f02',  // Orange
            3: '#7570b3',  // Purple
            4: '#e7298a'   // Magenta
        },
        defaultFillColor: '#777777'
    }
};

// Mapbox access token
mapboxgl.accessToken = 'pk.eyJ1Ijoibm92YTE3N3J1cyIsImEiOiJja3oyc2Q4Y3UwMTVuMnZwMjFiOWl2eHo1In0.cpXR0UPWNtpLKonGRe5hpA';

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

// Track current popup for cleanup
let currentPopup = null;

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

function updateTiles(uuid) {
    const zoom = Math.floor(map.getZoom());
    const center = map.getCenter();
    const centerTile = lngLatToTile(center.lng, center.lat, zoom);
    const tilesUrlTemplate = `${CONFIG.API_BASE_URL}/tiles/polygons/{z}/{x}/{y}/${uuid}`;

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
        // Add fill layer for polygons with level-based coloring
        map.addLayer({
            id: 'polygons-layer',
            type: 'fill',
            source: 'polygons-source',
            'source-layer': 'polygons',
            paint: {
                'fill-color': [
                    'match',
                    ['get', 'level'],
                    1, CONFIG.LAYER_SETTINGS.levelColors[1],
                    2, CONFIG.LAYER_SETTINGS.levelColors[2],
                    3, CONFIG.LAYER_SETTINGS.levelColors[3],
                    4, CONFIG.LAYER_SETTINGS.levelColors[4],
                    CONFIG.LAYER_SETTINGS.defaultFillColor // fallback color
                ],
                'fill-opacity': CONFIG.LAYER_SETTINGS.fillOpacity
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

function initApp() {
    map.addControl(new mapboxgl.NavigationControl(), 'top-right');
    map.addControl(new mapboxgl.FullscreenControl(), 'top-right');

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

    // Add popup on click
    map.on('click', 'polygons-layer', (e) => {
        if (e.features.length > 0) {
            const feature = e.features[0];
            const properties = feature.properties;

            let popupContent = '<div class="popup-content">';
            for (const [key, value] of Object.entries(properties)) {
                popupContent += `<strong>${key}:</strong> ${value}<br>`;
            }
            popupContent += '</div>';

            // Close existing popup before creating a new one
            closePopup();

            currentPopup = new mapboxgl.Popup({ closeButton: false })
                .setLngLat(e.lngLat)
                .setHTML(popupContent)
                .addTo(map);
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