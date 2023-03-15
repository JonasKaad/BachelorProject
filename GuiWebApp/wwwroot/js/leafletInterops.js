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
  map = L.map("map", {
    center: [lng, lat],
    zoom: zoomValue,
    layers: [osmLayer],
  });

  layerControl = L.control.layers(baseMap).addTo(map);
}

async function addPoint(lat, lng) {
  L.marker([lat, lng]).addTo(map);
}

async function createPath(coordinates, color) {
  var polyline = L.polyline(coordinates, { color: color }).addTo(map);
  map.fitBounds(polyline.getBounds());
}
