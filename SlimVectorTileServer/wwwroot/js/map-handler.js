// Mapbox access token
mapboxgl.accessToken = 'pk.eyJ1Ijoibm92YTE3N3J1cyIsImEiOiJja3oyc2Q4Y3UwMTVuMnZwMjFiOWl2eHo1In0.cpXR0UPWNtpLKonGRe5hpA';

// Initialize map
const map = new mapboxgl.Map({
    container: 'map',
    style: 'mapbox://styles/mapbox/streets-v11',
    center: [-74.6071028, 40.6931568],
    zoom: 2,
    minZoom: 2,
    maxZoom: 15,
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
 * Update map tiles with the given UUID
 * @param {string} uuid - UUID for the tile request
 */
function updateTiles(uuid) {
    const zoom = Math.floor(map.getZoom());
    const center = map.getCenter();
    const bounds = map.getBounds();
    const centerTile = lngLatToTile(center.lng, center.lat, zoom);
    const tilesUrlTemplate = 'http://localhost:5035/api/tiles/{z}/{x}/{y}/'+uuid+'.mvt';

    try {
        map.addSource('sites-source', {
            type: 'vector',
            tiles: [tilesUrlTemplate],
            minzoom: 2,
            maxzoom: 22
        });
    }
    catch (error) {
        console.log('Failed to add vector tile Source: ' + error.toString());
    }

    try {
        map.addLayer({
            id: 'sites-layer',
            type: 'circle',
            source: 'sites-source',
            'source-layer': 'sites',
            paint: {
                'circle-radius': 3,
                'circle-color': '#ff0000'
            }
        });
    }
    catch (error) {
        console.log('Failed to add vector tile Layer: ' + error.toString());
    }
}

/**
 * Create request parameters and send to the server
 * @returns {Promise<string|null>} UUID from the server response or null if failed
 */
async function createRequestParams() {
    const requestParams = document.getElementById('requestParams').value;

    try {
        const parsedParams = JSON.parse(requestParams);

        const response = await fetch('http://localhost:5035/api/tiles/request-params', {
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
        console.error('Error:', error);
        return null;
    }
}

// Event listeners
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('submitButton').addEventListener('click', () => {
        createRequestParams().then(uuid => {
            if (uuid) {
                cleanUpMap();
                updateTiles(uuid);
            } else {
                console.error('Failed to get Request Params UUID');
            }
        });
    });
});