// Configuration
const CONFIG = {
    // Replace API_BASE_URL with the actual value in your case.
    API_BASE_URL: '',
    MAP_SETTINGS: {
        initialCenter: [-74.6071028, 40.6931568],
        initialZoom: 0,
        minZoom: 0,
        maxZoom: 15
    },
    LAYER_SETTINGS: {
        circleRadius: 3,
        circleColor: '#ff0000'
    }
};

// Replace this with your own token or implement a more secure approach.
// mapboxgl.accessToken = '@Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN")';
mapboxgl.accessToken = '';

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

function lngLatToTile(lng, lat, zoom) {
    const tileCount = Math.pow(2, zoom);
    const x = Math.floor((lng + 180) / 360 * tileCount);
    const y = Math.floor((1 - Math.log(Math.tan(lat * Math.PI / 180) + 1 / Math.cos(lat * Math.PI / 180)) / Math.PI) / 2 * tileCount);
    return { x, y };
}

function cleanUpMap() {
    if (map.getLayer('sites-layer')) {
        map.removeLayer('sites-layer');
    }
    if (map.getSource('sites-source')) {
        map.removeSource('sites-source');
    }
}

function showNotification(message, type = 'error') {
    console.log(`${type.toUpperCase()}: ${message}`);
}

function updateTiles(uuid) {
    const zoom = Math.floor(map.getZoom());
    const center = map.getCenter();
    const bounds = map.getBounds();
    const centerTile = lngLatToTile(center.lng, center.lat, zoom);
    const cluster = getClusterOption(zoom);

    const tilesUrlTemplate = `${CONFIG.API_BASE_URL}/tiles/{z}/{x}/{y}/${cluster}/${uuid}`;

    try {
        map.addSource('sites-source', {
            type: 'vector',
            tiles: [tilesUrlTemplate],
            minzoom: CONFIG.MAP_SETTINGS.minZoom,
            maxzoom: CONFIG.MAP_SETTINGS.maxZoom + 7
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

async function createRequestParams() {
    const requestParams = document.getElementById('requestParams').value;

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
}

function getClusterOption(zoom) {
    return zoom < 10 ? 1 : 0;
}

document.addEventListener('DOMContentLoaded', initApp);
