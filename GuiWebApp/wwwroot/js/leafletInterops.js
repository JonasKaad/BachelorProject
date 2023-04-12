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
let currentColor = '';

//async function createPath(coordinates, color) {
//    var polyline = L.polyline(coordinates, { color: color, snakingSpeed: 600 }).addTo(map)
//    polyline.snakeIn();
//    L.featureGroup(getArrows(coordinates, 'black', 1, 5)).addTo(map);
//    currentCoords = coordinates;
//    currentColor = color;
//    map.fitBounds(L.latLngBounds(coordinates));
//}
async function createPath(coordinates, color) {
    var hotlineLayer = L.hotline(coordinates, {
        min: 1000,
        max: 42000,
        palette: {
            0.0: '#008800',
            0.5: '#ffff00',
            1.0: '#ff0000'
        },
        weight: 5,
        outlineColor: '#000000',
        outlineWidth: 1
    }).addTo(map)
    hotlineLayer.snakeIn();
    L.featureGroup(getArrows(coordinates, 'black', 1, 5)).addTo(map);
    currentCoords = coordinates;
    currentColor = color;
    map.fitBounds(L.latLngBounds(coordinates));





    ['weight', 'outlineWidth', 'min', 'max', 'smoothFactor'].forEach(function (value) {
        document.getElementById(value).addEventListener('input', function () {
            var style = {};
            style[value] = parseInt(this.value, 10);
            hotlineLayer.setStyle(style).redraw();
        });
    });

    document.getElementById('outlineColor').addEventListener('input', function () {
        hotlineLayer.setStyle({ 'outlineColor': this.value }).redraw();
    });

    var paletteColor1 = document.getElementById('paletteColor1');
    var paletteColor2 = document.getElementById('paletteColor2');
    var paletteColor3 = document.getElementById('paletteColor3');
    [paletteColor1, paletteColor2, paletteColor3].forEach(function (element) {
        element.addEventListener('input', updatePalette);
    });
    function updatePalette() {
        hotlineLayer.setStyle({
            'palette': {
                0.0: paletteColor1.value,
                0.5: paletteColor2.value,
                1.0: paletteColor3.value
            }
        }).redraw();
    }
}

async function resetView() {
    let currentCenter = map.getCenter();
    let currentZoom = map.getZoom();
    createMap(currentCenter.lat, currentCenter.lng, currentZoom);
}


async function redrawFlight() {
    resetView();
    L.polyline(currentCoords, { color: currentColor, snakingSpeed: 600 }).addTo(map).snakeIn();
}

async function redrawHoldingPattern(coordinates, color) {
    L.polyline(coordinates, { color: color, snakingSpeed: 200 }).addTo(map).snakeIn();

    let corner1 = coordinates[0];
    let corner2 = coordinates[coordinates.length - 1];
    console.log("C1: " + corner1 + " C2: " + corner2);
}
