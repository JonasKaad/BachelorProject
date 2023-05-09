var map;

var osmLayer = L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: "© OpenStreetMap",
});

var baseMap = {
    OpenStreetMap: osmLayer,
};

var layerControl;

async function createMap(lat, lng, zoomValue) {
    if (map !== undefined) {
        map.remove();
    }
    map = L.map("map", {
        center: [lat, lng],
        zoom: zoomValue,
        layers: [osmLayer],
    });
    L.control.measure().addTo(map);
}

async function addPoint(lat, lng) {
    L.marker([lat, lng]).addTo(map);
}

async function addWayPoints(lat, lng, name) {
    var myIcon = L.icon({
        iconUrl: 'waypoint.svg',
        iconSize: [20, 20],
    });
    var latitude = lat;
    latitude = +latitude.toFixed(2);
    var longitude = lng;
    longitude = +longitude.toFixed(2);
    let str = name + "<br>" + "Lat: " + latitude + "<br>" + "Long: " + longitude;
    L.marker([lat, lng], { icon: myIcon }, { title: str }).addTo(map).bindPopup(str);
}

let currentCoords = [];
let currentColor = '';

async function createPath(coordinates, color) {
    var polyline = L.polyline(coordinates, { color: color, snakingSpeed: 1500 }).addTo(map)
    polyline.snakeIn();
    L.featureGroup(getArrows(coordinates, 'black', 1, 5)).addTo(map);
    currentCoords = coordinates;
    currentColor = color;
    map.fitBounds(L.latLngBounds(coordinates));
}
async function resetView() {
    let currentCenter = map.getCenter();
    let currentZoom = map.getZoom();
    createMap(currentCenter.lat, currentCenter.lng, currentZoom);
}


async function redrawFlight() {
    resetView();
    L.polyline(currentCoords, { color: currentColor, snakingSpeed: 1500 }).addTo(map).snakeIn();
}
