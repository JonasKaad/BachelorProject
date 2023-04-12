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

async function createPath(coordinates, color) {
    var polyline = L.polyline(coordinates, { color: color, className: 'animate' }).addTo(map);
    L.featureGroup(getArrows(coordinates, 'black', 1, 8)).addTo(map);
    map.fitBounds(polyline.getBounds());
}
async function reanimatePath() {
    document.querySelectorAll("path.leaflet-interactive.animate").forEach(x => { x.style.animationName = "dummy"; setTimeout(function () { x.style.animationName = "dash" }, 10) });
}