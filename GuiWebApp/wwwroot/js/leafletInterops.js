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
}

async function addPoint(lat, lng) {
    L.marker([lat, lng]).addTo(map);
}
async function reanimatePath() {
    document.querySelectorAll("path.leaflet-interactive.animate").forEach(x => { x.style.animationName = "dummy"; setTimeout(function () { x.style.animationName = "dash" }, 10) });
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

async function createPath(coordinates, color) {
    //var polyline = L.polyline(coordinates, { color: color, className: 'animate' }).addTo(map);
    var polyline = L.polyline(coordinates, { color: color, snakingSpeed: 600 }).addTo(map).snakeIn();
    //var po = L.polyline(coordinates).addTo(map);
    //L.featureGroup(getArrows(coordinates, 'black', 1, 5)).addTo(map);
    //map.fitBounds(L.latLngBounds(corner1, corner2), true);
    // map.flyToBounds(L.latLngBounds(corner1, corner2));
    //map.zoomOut();
    currentCoords = coordinates;
    map.fitBounds(L.latLngBounds(coordinates));
    //map.setZoom(6);
}

async function resetView() {
    let currentCenter = map.getCenter();
    let currentZoom = map.getZoom();
    createMap(currentCenter.lat, currentCenter.lng, currentZoom);
}


async function redrawFlight() {
    resetView();
    L.polyline(currentCoords, { color: "red", snakingSpeed: 600 }).addTo(map).snakeIn();
}

async function redrawHoldingPattern(coordinates, color) {
    L.polyline(coordinates, { color: color, snakingSpeed: 200 }).addTo(map).snakeIn();

    let corner1 = coordinates[0];
    let corner2 = coordinates[coordinates.length - 1];
    console.log("C1: " + corner1 + " C2: " + corner2);
}
